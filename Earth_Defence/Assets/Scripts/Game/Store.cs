using System;
using System.Collections.Generic;
using UnityEngine;

namespace EarthDef
{
    /// <summary>
    /// 3차 규칙: 다이아 · 상점(상자 · 스태미나 · 골드 · 무료/광고 상자) · 기지 외형 · 터렛 스킨 · 결제 [초안] · 구글 계정/클라우드 [초안] · 도전 입장.
    /// [초안] = 외부 연동(결제 · 광고 · 구글 로그인)을 붙이기 전 가짜 흐름 — 에디터는 바로 성공, 폰은 결제를 꺼 둠 (출시 단계에서 실제 연동).
    /// </summary>
    public static class Store
    {
        static SaveData S => SaveData.Cur;

        // ---------- 다이아 ----------
        public static bool Pay(int dia) { if (S.dia < dia) return false; S.dia -= dia; S.Save(); return true; }

        // ---------- 상점 ----------
        public static int Left(ShopItemRow it) => it.perDay <= 0 ? 99 : it.perDay - S.Daily("shop:" + it.id);
        public static bool CanBuy(ShopItemRow it) => Left(it) > 0 && S.dia >= it.costDia;

        /// <summary>상점 물건 사기 → 받은 것 글자 목록 (못 사면 null)</summary>
        public static List<GotItem> Buy(ShopItemRow it, System.Random rnd = null)
        {
            if (!CanBuy(it)) return null;
            if (it.kind == "ad_box" && !Ads.Draft) { }   // 광고는 부른 쪽에서 (Ads.Show) 성공한 뒤에 Buy
            S.dia -= it.costDia;
            if (it.perDay > 0) S.DailyAdd("shop:" + it.id);
            rnd ??= new System.Random(Environment.TickCount);
            var got = new List<GotItem>();
            int n = Mathf.Max(1, (int)it.value);
            SaveData.BeginBatch();      // 10+1 상자: 가방이 넘쳐 분해될 때마다 저장하던 것을 한 번으로
            try { Give(it, n, rnd, got); } finally { SaveData.EndBatch(); }
            Audio.Play(Audio.Sfx.Merge);
            S.Save();
            return got;
        }

        static void Give(ShopItemRow it, int n, System.Random rnd, List<GotItem> got)
        {
            switch (it.kind)
            {
                case "gear_box": for (int i = 0; i < n; i++) { var g = Gear.NewGear(PityGrade(ref S.pityGear, rnd), null, rnd); got.Add(GotItem.Of(g)); } break;
                case "chip_box": for (int i = 0; i < n; i++) { var c = Gear.NewChip(PityGrade(ref S.chipPity, rnd), null, rnd); got.Add(GotItem.Of(c)); } break;
                case "free_box":
                case "ad_box":
                    for (int i = 0; i < n; i++)
                    {
                        int g = Mathf.Min(BoxMax, Gear.DropGrade(Mathf.Max(1, S.best), false, rnd));   // 6차: 무료 · 광고 상자도 전설까지
                        if (rnd.NextDouble() < 0.5) { var x = Gear.NewGear(g, null, rnd); got.Add(GotItem.Of(x)); }
                        else { var c = Gear.NewChip(g, null, rnd); got.Add(GotItem.Of(c)); }
                    }
                    break;
                case "stamina": Meta.AddStamina((int)it.value); got.Add(GotItem.Res("energy", $"{L.T("r_stamina")} +{it.value:0}")); break;
                // 요청 사항_05: 강화 재료(부품) 상자 · 터렛 조각 상자 (다이아 · 하루 10번)
                case "parts_box": S.parts += it.value; got.Add(GotItem.Res("parts", $"{L.T("r_parts")} +{it.value:0}")); break;
                case "shard_box":
                {
                    var open = Tables.Turrets.FindAll(t => S.Unlocked(t));
                    var cnt = new Dictionary<string, int>();
                    for (int i = 0; i < (int)it.value; i++) { var t = open[rnd.Next(open.Count)]; S.shards[S.TIdx(t.id)]++; cnt[t.id] = cnt.TryGetValue(t.id, out var k) ? k + 1 : 1; }
                    foreach (var kv in cnt) got.Add(new GotItem { name = $"{Tables.TurretById[kv.Key].name} {L.T("r_shards")} +{kv.Value}", icon = "shard", turret = kv.Key });
                    break;
                }
                case "gold":
                {
                    double g = Math.Floor(it.value * S.PatrolGoldHour);     // 밸런스 2차: 순찰과 같은 식 (예전엔 스테이지 곱 성장이 빠져 후반에 아주 작았음)
                    S.gold += g; got.Add(GotItem.Res("coin", $"{L.T("r_gold")} +{Num.F(g)}")); break;
                }
            }
        }

        /// <summary>상자 등급 (Shop.xlsx Box_Rate 확률 그대로 — 화면 [확률] 과 같음)</summary>
        public static int BoxGrade(System.Random rnd)
        {
            int max = BoxMax;
            double sum = 0; foreach (var (g, r) in Tables.BoxRates) if (g <= max) sum += r;
            double x = rnd.NextDouble() * sum;
            foreach (var (g, r) in Tables.BoxRates) { if (g > max) continue; x -= r; if (x < 0) return g; }
            return 1;
        }

        // 6차 수정안 "상자 뽑기는 전설까지 (신화부터는 합성으로만) · 55뽑이면 전설 100% (천장) — 못 뽑으면 쌓이고 전설을 뽑으면 처음부터"
        /// <summary>상자에서 나오는 가장 높은 등급 (Config Box_Grade_Max 5 = 전설)</summary>
        public static int BoxMax => Mathf.Clamp(Tables.Ci("Box_Grade_Max", 5), 1, Tables.GradeMax);
        /// <summary>천장 등급 = 전설(5) — 상한이 더 낮으면 상한</summary>
        public static int PityTarget => Mathf.Min(5, BoxMax);
        public static int PityN => Tables.Ci("Box_Pity", 55);
        /// <summary>이 상자 종류로 전설 확정까지 남은 횟수 (이번 회 포함: 1 = 다음 번이 확정)</summary>
        public static int PityLeft(string kind) => Mathf.Max(1, PityN - (kind == "chip_box" ? S.chipPity : S.pityGear));
        /// <summary>한 번 뽑기: 횟수를 하나 쌓고, 천장에 닿으면 전설 · 전설 이상이 나오면 셈을 처음부터 (10+1 은 11번으로 셈)</summary>
        public static int PityGrade(ref int cnt, System.Random rnd)
        {
            int g = BoxGrade(rnd);
            cnt++;
            if (PityN > 0 && cnt >= PityN && g < PityTarget) g = PityTarget;
            if (g >= PityTarget) cnt = 0;
            return g;
        }

        // 월 1회 상품 (6차: 다이아 폭탄 패키지) — 세이브 칸을 새로 만들지 않고 iapOnce 에 "month:상품:연월"
        static string MonthKey(ProductRow p) => "month:" + p.id + ":" + DateTimeOffset.FromUnixTimeSeconds(SaveData.Clock).ToLocalTime().ToString("yyyyMM");
        public static bool MonthDone(ProductRow p) => p.kind == "monthly" && S.iapOnce.Contains(MonthKey(p));

        // ---------- 결제 [초안] ----------
        /// <summary>폰에서는 결제 연동(라이선스 키 · 상품)이 갖춰지기 전까지 꺼 둠 (카치건 방식). 에디터는 가짜 결제로 바로 지급</summary>
        public static bool IapReady => Application.isEditor;
        public static bool FirstDouble(ProductRow p) => p.firstDouble && !S.iapOnce.Contains("first2x:" + p.id);
        /// <summary>광고 제거 (요청 사항_05) — 세이브 판 번호를 안 올리려고 iapOnce 목록에 "noads" 로 기록</summary>
        public static bool NoAds => S != null && S.iapOnce.Contains("noads");
        // 골드 부스터 (밸런스 3차): 산 날부터 Gold_Boost_Days 일 동안 판 골드 x Gold_Boost_Mul. 세이브 칸을 새로 만들지 않고 iapOnce 에 "goldboost:끝나는 시각"
        const string BoostKey = "goldboost:";
        public static long GoldBoostUntil
        {
            get { if (S == null) return 0; foreach (var k in S.iapOnce) if (k.StartsWith(BoostKey) && long.TryParse(k.Substring(BoostKey.Length), out var t)) return t; return 0; }
        }
        public static bool GoldBoostOn => SaveData.Clock < GoldBoostUntil;
        public static int GoldBoostDaysLeft => (int)Math.Ceiling(Math.Max(0, GoldBoostUntil - SaveData.Clock) / 86400.0);

        public static int Purchase(ProductRow p)
        {
            if (!IapReady) return 0;
            if (p.kind == "boost")
            {   // 남은 기간이 있으면 이어 붙임
                long until = Math.Max(SaveData.Clock, GoldBoostUntil) + (long)Tables.Ci("Gold_Boost_Days", 30) * 86400;
                S.iapOnce.RemoveAll(k => k.StartsWith(BoostKey)); S.iapOnce.Add(BoostKey + until);
                Audio.Play(Audio.Sfx.Coin); S.Save(); return 1;
            }
            if (p.kind == "noads")
            {
                if (NoAds) return 0;
                S.iapOnce.Add("noads"); Audio.Play(Audio.Sfx.Coin); S.Save(); return 1;
            }
            if (p.kind == "monthly")
            {
                if (MonthDone(p)) return 0;
                S.iapOnce.Add(MonthKey(p));
            }
            int n = p.dia * (FirstDouble(p) ? 2 : 1);
            if (FirstDouble(p)) S.iapOnce.Add("first2x:" + p.id);
            S.dia += n;
            Audio.Play(Audio.Sfx.Coin);
            S.Save();
            return n;
        }

        // ---------- 기지 외형 ----------
        public static FrameRow Frame => Tables.Frame(S.frame);
        public static int FrameStar => S.FrameStar(S.frame);
        public static bool BuyFrame(FrameRow f)
        {
            if (S.FrameStar(f.id) > 0 || !Pay(f.price)) return false;
            S.SetFrameStar(f.id, 1); S.Save(); Audio.Play(Audio.Sfx.Upgrade); return true;
        }
        public static double FrameUpGold(string id) => Tables.C("Frame_Up_Gold", 20000) * Math.Pow(3, Mathf.Max(0, S.FrameStar(id) - 1));
        public static bool CanUpFrame(string id) => id != "basic" && S.FrameStar(id) is > 0 and < 5 && S.gold >= FrameUpGold(id);
        public static bool UpFrame(string id)
        {
            if (!CanUpFrame(id)) return false;
            S.gold -= FrameUpGold(id); S.SetFrameStar(id, S.FrameStar(id) + 1); S.Save(); Audio.Play(Audio.Sfx.Upgrade); return true;
        }
        public static void EquipFrame(string id) { if (S.FrameStar(id) <= 0) return; S.frame = id; S.Save(); World.I?.SetFrame(Tables.Frame(id)); }

        // ---------- 터렛 스킨 ----------
        public static bool BuySkin(SkinRow k)
        {
            if (S.HasSkin(k.turret) || !Pay(k.price)) return false;
            S.skins.Add(k.turret); if (!S.skinOn.Contains(k.turret)) S.skinOn.Add(k.turret);
            S.Save(); Audio.Play(Audio.Sfx.Merge); return true;
        }
        public static void ToggleSkin(string turret)
        {
            if (!S.HasSkin(turret)) return;
            if (!S.skinOn.Remove(turret)) S.skinOn.Add(turret);
            S.Save();
        }

        // ---------- 도전 입장 ----------
        public static int Tickets(string mode) => (mode == "endless" ? Tables.Ci("Endless_Tickets", 3) : Tables.Ci("Survival_Tickets", 2)) - S.Daily("mode:" + mode);
        public static int TicketMax(string mode) => mode == "endless" ? Tables.Ci("Endless_Tickets", 3) : Tables.Ci("Survival_Tickets", 2);

        // ---------- 구글 계정 · 클라우드 [초안] ----------
        const string CloudKey = "EarthDef.Cloud.Draft";
        public static bool Linked => !string.IsNullOrEmpty(S.account);
        public static void Link() { S.account = "draft-user"; S.Save(); }
        public static void Unlink() { S.account = ""; S.Save(); }
        public static bool HasCloud => PlayerPrefs.HasKey(CloudKey);
        public static void CloudSave() { S.Save(); PlayerPrefs.SetString(CloudKey, JsonUtility.ToJson(S)); PlayerPrefs.Save(); }
        public static SaveData CloudPeek()
        {
            if (!HasCloud) return null;
            try { return JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(CloudKey)); } catch { return null; }
        }
        /// <summary>클라우드 판으로 바꿈 → 게임을 로비부터 다시</summary>
        public static bool CloudLoad()
        {
            var c = CloudPeek(); if (c == null) return false;
            PlayerPrefs.SetString(SaveData.Key, PlayerPrefs.GetString(CloudKey)); PlayerPrefs.Save();
            SaveData.Load();
            Game.ToLobby();
            return true;
        }
    }
}
