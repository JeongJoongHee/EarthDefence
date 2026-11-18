using UnityEngine;

namespace EarthDef
{
    /// <summary>
    /// 로비에서 기지(돔) 앞을 대장 카피가 좌우로 어슬렁어슬렁 걸어다님 (11-16 저녁 사용자 의견).
    /// 걷다가 멈춰 1~3초 쉬고(숨쉬기 · 가끔 고개 젖혀 하늘 보기) 다른 자리로 · 가는 쪽을 봄 · 총은 비스듬히 메고 있음.
    /// 전투가 시작되면 사라짐 (Game.StartStage). 걸음 · 뒤뚱은 전투의 TurretUnit.Move 와 같은 식.
    /// 7차 수정안: 로비에 사령탑 · 달 바닥이 없어지고 큰 지구 → 지구 꼭대기 둥근 면 위를 걸음 (선 자리의 기울기만큼 몸도 기움)
    /// </summary>
    public class LobbyWalker : MonoBehaviour
    {
        public static LobbyWalker I;
        SpriteRenderer body;
        Transform gunRoot;
        float x, target, wait = 1.2f, vel, walkPh, t, lean, look;
        bool left;
        float baseY;
        public float X => x;
        public bool Walking => wait <= 0;
        /// <summary>시험용: 바로 tx 로 걷기 시작</summary>
        public void Go(float tx) { wait = 0; target = tx; left = tx < x; }

        public static void Show(bool on)
        {
            if (!on) { if (I) Destroy(I.gameObject); I = null; return; }
            if (I) return;
            var go = new GameObject("LobbyWalker");
            I = go.AddComponent<LobbyWalker>();
            I.Build();
        }

        void Build()
        {
            x = target = Tables.Cf("Lobby_Walk_X", 0f) + Random.Range(-0.8f, 0.8f);   // 7차: 지구 꼭대기 가운데
            baseY = World.EarthSurf(x) - 0.06f;
            transform.position = new Vector3(x, baseY, 0);
            body = gameObject.AddComponent<SpriteRenderer>();
            body.sprite = Art.Guardian; body.sortingOrder = 5;
            gunRoot = new GameObject("GunRoot").transform; gunRoot.SetParent(transform, false);
            var g = new GameObject("Gun"); g.transform.SetParent(gunRoot, false);
            var gs = g.AddComponent<SpriteRenderer>(); gs.sprite = Art.GuardianGun; gs.sortingOrder = 6;
            Pose();
        }

        void Pose()
        {
            float m = left ? -1 : 1;
            body.flipX = left;
            gunRoot.localPosition = new Vector3(Art.GuardianHand.x * m, Art.GuardianHand.y, 0);
            gunRoot.localScale = new Vector3(m, 1, 1);
            float rest = 32f + look * 18f + Mathf.Sin(walkPh * 2f) * 3f;      // 비스듬히 메고 걷기 · 하늘 볼 땐 조금 듦
            gunRoot.localRotation = Quaternion.Euler(0, 0, (left ? -rest : rest) - transform.localEulerAngles.z);
        }

        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            t += dt;
            float max = Tables.Cf("Lobby_Walk_Range", 1.8f), cx = Tables.Cf("Lobby_Walk_X", 0f);
            if (wait > 0)
            {
                wait -= dt;
                if (wait <= 0)
                {
                    // 다음 자리: 너무 가까우면 다시 고름
                    for (int i = 0; i < 5 && Mathf.Abs(target - x) < 0.6f; i++) target = cx + Random.Range(-max, max);
                    left = target < x;
                }
            }
            x = Mathf.SmoothDamp(x, target, ref vel, 0.5f, 0.9f, dt);
            float sp = Mathf.Abs(vel);
            if (wait <= 0 && Mathf.Abs(target - x) < 0.03f && sp < 0.05f) { wait = Random.Range(1f, 3f); look = Random.value < 0.35f ? 1 : 0; }
            if (sp > 0.05f) walkPh += sp * dt * 9f;
            else walkPh = Mathf.MoveTowards(walkPh, Mathf.Round(walkPh / Mathf.PI) * Mathf.PI, dt * 4f);
            float bob = Mathf.Abs(Mathf.Sin(walkPh)) * Mathf.Min(0.06f, sp * 0.12f);
            float breathe = sp < 0.05f ? Mathf.Sin(t * 2.2f) * 0.008f : 0f;
            baseY = World.EarthSurf(x) - 0.06f;
            float tilt = World.EarthTilt(x);
            transform.position = new Vector3(x, baseY + bob, 0);
            transform.localScale = new Vector3(1, 1 + breathe, 1);
            lean = Mathf.Lerp(lean, Mathf.Clamp(-vel * 9f, -6f, 6f) + (wait > 0 ? look * (left ? -6f : 6f) : 0f), Mathf.Min(1, 6f * dt));
            transform.localRotation = Quaternion.Euler(0, 0, tilt + lean + Mathf.Sin(walkPh) * 2.5f * Mathf.Min(1, sp * 2f));
            if (wait > 0 && look > 0 && wait < 0.6f) look = Mathf.MoveTowards(look, 0, dt * 2f);
            Pose();
        }

        void OnDestroy() { if (I == this) I = null; }
    }
}
