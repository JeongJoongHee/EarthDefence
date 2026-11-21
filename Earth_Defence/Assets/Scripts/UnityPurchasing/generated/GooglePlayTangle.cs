// 11-21 자리만 잡아 둔 파일 — 결제 라이선스 키를 넣으면(메뉴 지구 방위대 › 결제 라이선스 키 넣기) Unity IAP 가 이 파일을 새로 만듦.
// 키가 없으면 IsPopulated = false → 폰에서 결제가 꺼져 있음 (Iap.Configured)

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        public static readonly bool IsPopulated = false;

        public static byte[] Data() {
            return null;
        }
    }
}
