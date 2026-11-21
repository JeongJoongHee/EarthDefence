using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using System.Linq;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;
#endif

namespace EarthDef
{
    /// <summary>
    /// 인앱 결제 (11-21 비공개 테스트 준비 — 카치건 Iap 를 가져옴 · Unity In-App Purchasing 5.4.4 · 구글 결제 라이브러리).
    /// 상품 = Product 표 8줄. 구글 상품 ID = Product_Id 칸 그대로 (Play Console 인앱 상품 ID 와 똑같아야 함).
    ///  - 영구(한 번 사면 계속): Kind noads (광고 제거) — 앱을 지웠다 다시 깔아도 구글 구매 기록으로 되살림
    ///  - 소모: Kind dia (다이아) · monthly (월 1회 다이아 폭탄) · boost (골드 부스터 30일)
    ///  - 지급 내용은 Store.Grant 하나 (에디터 가짜 결제 · 폰 결제 · 되살리기 공통)
    ///
    ///  [구매] → 구글 결제창 → OnPurchasePending → 앱 안에서 구글 서명 확인 (CrossPlatformValidator, 라이선스 키)
    ///  → 지급 · 세이브 → 구글에 "지급 끝"(ConfirmPurchase).
    ///  - 결제 직후 앱이 꺼지면 다음 실행 때 지난 결제를 가져와 같은 흐름으로 지급 (구글은 "지급 끝"을 못 받은 결제를 3일 뒤 환불)
    ///  - 같은 주문을 두 번 주지 않게 SaveData.iapOnce 에 "order:주문 번호" (세이브 판 번호 그대로)
    ///
    /// 켜지는 조건: 안드로이드 기기 + 라이선스 키(GooglePlayTangle — 메뉴 지구 방위대 › 결제 라이선스 키 넣기) + 스토어에 상품이 있음.
    /// 키가 없으면 폰에선 결제가 꺼져 있고 원화 단추가 회색("결제 준비 중"). 에디터는 가짜 결제(바로 지급).
    /// </summary>
    public static class Iap
    {
        /// <summary>상품 정보를 받았거나 지급했을 때 — 상점이 다시 그림</summary>
        public static event Action Changed;
        static SaveData S => SaveData.Cur;

        public static bool Permanent(ProductRow p) => p != null && p.kind == "noads";
        public static bool Owned(ProductRow p) => Permanent(p) && Store.NoAds;
        static ProductRow Row(string productId) => Tables.Products.Find(x => x.productId == productId);

        /// <summary>상점 다이아 칸 위 작은 글</summary>
        public static string Note => Application.isEditor ? L.T("iap_draft") : Configured ? "" : L.T("iap_off");

        /// <summary>단추 글자: 스토어 가격(받았으면 — "₩5,900") → 표의 원화 "5,900원"</summary>
        public static string PriceText(ProductRow p) => StorePrice(p.productId) ?? Num.Comma(p.krw) + "원";

        /// <summary>[구매] 기다리는 지급 알림 (상품 ID → 단추마다 다른 토스트)</summary>
        static readonly Dictionary<string, Action<int>> waiting = new();

        /// <summary>지급 (에디터 가짜 결제 · 폰 결제 · 되살리기 공통) — 받은 다이아 수(광고 제거 · 부스터는 1)</summary>
        static int Grant(ProductRow p, bool restore = false)
        {
            int n = Store.Grant(p, restore);
            Debug.Log($"[Iap] {(restore ? "되살림" : "지급")} {p.productId} ({p.kind}) → {n}");
            if (waiting.TryGetValue(p.productId, out var cb)) { waiting.Remove(p.productId); cb?.Invoke(n); }
            else if (n > 0) Toast.Show(restore ? L.F("iap_restored", p.name) : L.F("iap_done", p.name));
            Changed?.Invoke();
            Game.Lobby?.Refresh();
            return n;
        }

        /// <summary>상점 [구매]. done(n) = 지급됐을 때 (폰은 결제창이 닫히고 확인이 끝난 뒤 · 취소하면 안 부름)</summary>
        public static void Buy(ProductRow p, Action<int> done)
        {
            if (p == null) return;
            if (Owned(p)) { Toast.Show(L.T("noads_done")); return; }
            if (Store.MonthDone(p)) { Toast.Show(L.T("month_done")); return; }
#if UNITY_ANDROID && !UNITY_EDITOR
            switch (Purchase(p))
            {
                case 0: waiting[p.productId] = done; break;   // 결제창 열림
                case 1: Toast.Show(L.T("iap_connecting")); break;
                default: Toast.Show(L.T("iap_off")); break;
            }
#else
            if (!CanSell(p)) { Toast.Show(L.T("iap_off")); return; }
            waiting[p.productId] = done;
            Grant(p);   // 에디터 가짜 결제 (자동 시험도)
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static StoreController store;
        static CrossPlatformValidator validator;
        static bool connecting, connected;
        static readonly Dictionary<string, Product> products = new();
        const int KeepOrders = 100;

        /// <summary>라이선스 키가 들어 있는지 (없으면 결제를 아예 안 켬 — 영수증 서명을 확인할 수 없으니까)</summary>
        public static bool Configured => GooglePlayTangle.IsPopulated;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() => Connect();

        static List<ProductDefinition> Definitions() =>
            Tables.Products.Where(p => !string.IsNullOrEmpty(p.productId)).GroupBy(p => p.productId).Select(g => g.First())
                .Select(p => new ProductDefinition(p.productId, Permanent(p) ? ProductType.NonConsumable : ProductType.Consumable)).ToList();

        static async void Connect()
        {
            if (connecting || connected) return;
            if (!Configured) { Debug.Log("[Iap] 라이선스 키 없음 → 결제 꺼짐 (지구 방위대 › 결제 라이선스 키 넣기)"); return; }
            if (Definitions().Count == 0) { Debug.Log("[Iap] 상품 없음 → 결제 꺼짐"); return; }
            connecting = true;
            MainThread.Get();
            try
            {
                if (validator == null) validator = new CrossPlatformValidator(GooglePlayTangle.Data(), Application.identifier);
                if (store == null)
                {
                    store = UnityIAPServices.StoreController();
                    store.OnStoreConnected += OnConnected;
                    store.OnStoreDisconnected += d => { connected = false; connecting = false; Debug.LogWarning("[Iap] 스토어 연결 끊김: " + d.message); };
                    store.OnProductsFetched += OnProducts;
                    store.OnProductsFetchFailed += f => Debug.LogWarning($"[Iap] 상품 정보 실패 {f.FailedFetchProducts.Count}개: {f.FailureReason}");
                    store.OnPurchasesFetched += OnPurchasesFetched;
                    store.OnPurchasesFetchFailed += f => Debug.LogWarning("[Iap] 지난 결제 가져오기 실패: " + f.message);
                    store.OnPurchasePending += GrantOrder;
                    store.OnPurchaseConfirmed += o => { if (o is FailedOrder f) Debug.LogWarning($"[Iap] '지급 끝' 알림 실패 {ProductIdOf(o)}: {f.FailureReason} {f.Details}"); };
                    store.OnPurchaseFailed += OnFailed;
                    store.OnPurchaseDeferred += _ => Toast.Show(L.T("iap_deferred"));
                }
                await store.Connect();
            }
            catch (Exception e)
            {
                connecting = false;
                Debug.LogWarning("[Iap] 연결 실패: " + e.Message);
            }
        }

        static void OnConnected()
        {
            connected = true;
            connecting = false;
            Debug.Log("[Iap] 스토어 연결됨");
            store.FetchProducts(Definitions());
        }

        static void OnProducts(List<Product> list)
        {
            foreach (var p in list)
                if (p != null && p.availableToPurchase) products[p.definition.id] = p;
            Debug.Log($"[Iap] 상품 {products.Count}개: " + string.Join(", ", products.Keys));
            Changed?.Invoke();
            Game.Lobby?.Refresh();
            store.FetchPurchases();   // 못 준 결제 → OnPurchasePending · 이미 산 영구 상품 → OnPurchasesFetched
        }

        /// <summary>이 상품을 팔 수 있는지: 키가 있고 스토어에서 상품 정보를 받았을 때 (Play Console 에 상품을 만들면 저절로 켜짐)</summary>
        public static bool CanSell(ProductRow p) => Configured && p != null && products.ContainsKey(p.productId);

        public static string StorePrice(string id)
        {
            if (string.IsNullOrEmpty(id) || !products.TryGetValue(id, out var p)) return null;
            var s = p.metadata?.localizedPriceString;
            return string.IsNullOrEmpty(s) ? null : s;
        }

        /// <returns>0 = 결제창 열림 · 1 = 연결 중 · 2 = 쓸 수 없음</returns>
        static int Purchase(ProductRow p)
        {
            if (!Configured) return 2;
            if (!connected || !products.TryGetValue(p.productId, out var product)) { Connect(); return 1; }
            store.PurchaseProduct(product);
            return 0;
        }

        static string ProductIdOf(Order order) => order?.CartOrdered?.Items()?.FirstOrDefault()?.Product?.definition?.id ?? "";
        static string OrderKey(string tx) => "order:" + tx;

        static void RememberOrder(string tx)
        {
            if (string.IsNullOrEmpty(tx)) return;
            S.iapOnce.Add(OrderKey(tx));
            var old = S.iapOnce.Where(k => k.StartsWith("order:")).ToList();
            for (int i = 0; i < old.Count - KeepOrders; i++) S.iapOnce.Remove(old[i]);
        }

        static void GrantOrder(PendingOrder order)
        {
            string id = ProductIdOf(order);
            string tx = order.Info?.TransactionID ?? "";
            var row = Row(id);
            if (row == null) { Debug.LogWarning($"[Iap] 표에 없는 상품 '{id}' — 지급 안 함 (Product 표 Product_Id 확인)"); return; }
            if (!string.IsNullOrEmpty(tx) && S.iapOnce.Contains(OrderKey(tx))) { store.ConfirmPurchase(order); return; }   // 지급은 끝났는데 "지급 끝"만 못 보낸 경우
            if (!ReceiptValid(order.Info?.Receipt, id)) { Toast.Show(L.T("iap_invalid")); waiting.Remove(id); return; }
            RememberOrder(tx);
            Grant(row);   // 안에서 세이브
            store.ConfirmPurchase(order);
        }

        /// <summary>이미 산 영구 상품(광고 제거) 되살리기 — 다시 설치 · 세이브 초기화 뒤</summary>
        static void OnPurchasesFetched(Orders orders)
        {
            if (orders?.ConfirmedOrders == null) return;
            foreach (var o in orders.ConfirmedOrders)
            {
                var row = Row(ProductIdOf(o));
                if (row == null || !Permanent(row) || Owned(row)) continue;
                if (!ReceiptValid(o.Info?.Receipt, row.productId)) continue;
                Grant(row, true);
            }
        }

        static bool ReceiptValid(string receipt, string productId)
        {
            try
            {
                foreach (var r in validator.Validate(receipt))
                    if (r.productID == productId) return true;
                Debug.LogWarning($"[Iap] 영수증에 '{productId}' 없음");
                return false;
            }
            catch (IAPSecurityException e) { Debug.LogWarning("[Iap] 영수증 서명 불일치: " + e.Message); return false; }
            catch (Exception e) { Debug.LogWarning("[Iap] 영수증 확인 실패: " + e.Message); return false; }
        }

        static void OnFailed(FailedOrder f)
        {
            string id = ProductIdOf(f);
            waiting.Remove(id);
            Debug.Log($"[Iap] 결제 실패 {id}: {f.FailureReason} {f.Details}");
            switch (f.FailureReason)
            {
                case PurchaseFailureReason.UserCancelled: Toast.Show(L.T("iap_cancel")); break;
                case PurchaseFailureReason.ExistingPurchasePending: Toast.Show(L.T("iap_pending")); break;
                default: Toast.Show(L.T("iap_fail")); break;
            }
        }
#else
        // 에디터 · 다른 플랫폼: 스토어 없음 — 에디터는 가짜 결제로 살 수 있게 늘 팖
        public static bool Configured => false;
        /// <summary>시험용: 에디터에서 "결제 키가 없는 휴대폰"처럼 원화 상품을 회색으로</summary>
        public static bool SimulateNoStore;
        public static bool CanSell(ProductRow p) => Application.isEditor && !SimulateNoStore && p != null;
        public static string StorePrice(string id) => null;
#endif
    }
}
