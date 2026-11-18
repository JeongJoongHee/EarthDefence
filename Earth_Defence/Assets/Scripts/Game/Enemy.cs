using UnityEngine;

namespace EarthDef
{
    /// <summary>
    /// 적 하나. 11-18 전투 배치 2판: 오른쪽 위에서 사령탑을 향해 비스듬히 날아와 방어선(Shield_X)에 닿으면:
    /// 운석(Crash)은 부딪쳐 터지며 큰 피해, 나머지는 멈춰서 계속 때림. 흔들림(zigzag · hover · boss)은 날아가는 줄의 옆으로.
    /// 꼬리 달린 운석은 날아가는 쪽으로 머리를 돌림.
    /// 움직임(Move): fall 곧게 · zigzag 흔들며 · fast 빠르게 · walk 천천히 · hover 좌우로 크게 · boss 느리게 + 부하 부르기.
    /// 상태이상: 둔화 · 마비 · 화상 · 취약. 맞으면 흰 실루엣이 잠깐 번쩍.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        public EnemyRow row;
        public double hp, maxHp, atk;
        public bool dead;
        public float radius;
        public float size;            // 그림 크기 (표 Size x 보스 배율)
        float barK = 1f;
        // 11-17 최적화: 위치를 C# 쪽에 들고 있음 — 목표 찾기 · 맞음 판정이 한 칸에 수천 번 읽는데, 매번 엔진(transform)에서 읽으면 느림
        Vector2 pos;
        public Vector2 Pos => pos;
        /// <summary>위치 바꾸기 (시험 · 시험장에서 직접 옮길 때도 이것으로)</summary>
        public void Teleport(Vector2 p) { pos = p; basePos = p - perp * sway; transform.position = p; }
        // 11-18: 날아가는 줄 (basePos 가 dir 로 나아가고 화면 위치 = basePos + 옆 흔들림)
        Vector2 basePos, dir = Vector2.down, perp = Vector2.right; float sway;
        public Vector2 Dir => dir;
        bool turnToDir;
        /// <summary>목표(사령탑 앞 한 점)를 향하게 — Battle.Spawn 이 부름</summary>
        public void Aim(Vector2 target)
        {
            var d = target - pos;
            dir = d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.left;
            perp = new Vector2(-dir.y, dir.x);
            if (turnToDir) transform.localRotation = baseRot = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f);
        }
        Quaternion baseRot = Quaternion.identity;
        /// <summary>방어선까지 남은 거리 (날아가는 방향으로)</summary>
        public float Remain => atShield ? 0f : dir.x < -0.05f ? Mathf.Max(0f, (pos.x - radius - World.ShieldX) / -dir.x) : Mathf.Max(0f, pos.x - radius - World.ShieldX) + 3f;
        /// <summary>예전 세로 전장의 높이로 바꾼 값 (= Shield_Y + 남은 거리) — Pos_Near_Y · Pos_Far_Y 같은 예전 기준을 그대로 쓰려고</summary>
        public float VY => World.ShieldY + Remain;

        SpriteRenderer sr, white;
        SpriteRenderer barBg, barFill;
        float flash, age, phase, hitT, summonT;
        bool atShield;
        // 상태이상
        public float slow, slowT, para, vulnT;
        public double vuln;
        float burnT, burnTick; double burnDps; TurretInst burnSrc;
        public bool Paralyzed => para > 0;
        public static int Knocks;   // 시험용: 밀어낸 횟수
        /// <summary>터렛 시험장 허수아비: 제자리에 서 있음</summary>
        public bool hold;
        public bool Debuffed => slow > 0 || para > 0 || burnT > 0 || vuln > 0;

        public void Init(EnemyRow r, Vector2 p, double hp, double atk)
        {
            row = r; this.hp = maxHp = hp; this.atk = atk;
            pos = basePos = p; transform.position = p;
            turnToDir = r.crash && r.pivotY < 0.4f;     // 꼬리 달린 운석 (그림이 아래로 떨어지는 모양)
            // 2차 수정안: 중간 보스 x3 · 보스 x3.5 (Config Elite_Size_Mul · Boss_Size_Mul)
            size = r.size * (r.IsBoss ? Tables.Cf("Boss_Size_Mul", 3.5f) : r.IsElite ? Tables.Cf("Elite_Size_Mul", 3f) : 1f);
            transform.localScale = Vector3.one * size;
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Art.Enemy(r); sr.sortingOrder = r.IsBoss ? 12 : r.IsElite ? 11 : 10;
            var w = new GameObject("White"); w.transform.SetParent(transform, false);
            white = w.AddComponent<SpriteRenderer>(); white.sprite = Art.Enemy(r, true); white.sortingOrder = sr.sortingOrder; white.color = new Color(1, 1, 1, 0);
            // 맞는 크기: 그림 242px(PPU200) = 1.21칸 → 반지름 약 0.42 x Size (보스 그림은 2배)
            radius = (r.IsBoss && r.art == "planet" ? 0.95f : 0.42f) * size;
            phase = Random.Range(0f, 6.28f);
            summonT = Tables.Cf("Boss_Summon_Gap", 4f);
            if (r.IsBoss || r.IsElite)
            {
                barBg = Bar("BarBg", Art.UI("track"), 20); barFill = Bar("BarFill", Art.UI("fill"), 21);
                barFill.color = Art.Red; hasBar = true;
            }
            SetAlpha(0); Alpha = 0;
        }

        SpriteRenderer Bar(string name, Sprite s, int order)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var b = go.AddComponent<SpriteRenderer>();
            b.sprite = s; b.drawMode = SpriteDrawMode.Sliced; b.sortingOrder = order;
            float top = (row.art == "planet" ? 1.25f : 0.75f);
            go.transform.localPosition = new Vector3(0, top, 0);
            barK = Mathf.Max(1f, size * 0.5f);                 // 큰 보스는 체력 줄도 크게
            go.transform.localScale = Vector3.one / size * barK;
            b.size = new Vector2(1.6f, 0.3f);
            return b;
        }

        bool shown;
        /// <summary>시험용: 지금 투명도</summary>
        public float Alpha { get; private set; } = 1f;
        void SetAlpha(float a) { var c = sr.color; c.a = a; sr.color = c; }

        public void Step(float dt)
        {
            if (dead) return;
            age += dt;
            if (!shown)
            {
                // 7차 "떨어지는 별똥별은 Lv 게이지 밑부터 Fade in": 그림 윗끝이 Lv 게이지 아래(World.FadeTop)를 지나면서 Enemy_Fade_Len 칸 동안 서서히
                //   (오른쪽 가장자리 · 큰 보스는 예전처럼 나온 뒤 0.4초)
                float a = Mathf.Clamp01(age / 0.4f);
                if (!row.IsBoss && !row.IsElite) a = Mathf.Min(a, Mathf.Clamp01((World.FadeTop - pos.y - 0.45f * size) / Tables.Cf("Enemy_Fade_Len", 1.2f)));
                SetAlpha(a); Alpha = a;
                if (a >= 1f) shown = true;
            }
            // 상태이상 시간
            if (slowT > 0) { slowT -= dt; if (slowT <= 0) slow = 0; }
            if (para > 0) para -= dt;
            if (vulnT > 0) { vulnT -= dt; if (vulnT <= 0) vuln = 0; }
            if (burnT > 0)
            {
                burnT -= dt; burnTick -= dt;
                if (burnTick <= 0) { burnTick = 0.5f; Combat.Raw(this, burnDps * 0.5, false, burnSrc); if (dead) return; }
            }
            if (flash > 0) { flash -= dt; white.color = new Color(1, 1, 1, Mathf.Clamp01(flash / 0.1f) * 0.85f); }

            // 밸런스 2차 보스 패턴: 체력이 Boss_Rage_At(50%) 아래로 처음 내려가면 화냄 — 부하를 한꺼번에 부르고 잠깐 돌진 (대장 카피를 옮겨 막는 순간)
            if (row.IsBoss && !raged && hp < maxHp * Tables.C("Boss_Rage_At", 0) && Battle.I)
            {
                raged = true; rageT = Tables.Cf("Boss_Rage_Time", 4f); Battle.I.bossRages++;
                int n = Tables.Ci("Boss_Rage_Summon", 6); string sid = string.IsNullOrEmpty(row.summon) ? "pebble" : row.summon;
                for (int i = 0; i < n; i++) Battle.I.Spawn(sid, Pos + new Vector2((i - (n - 1) / 2f) * 0.7f, 0.2f), true);
                Battle.I.Announce(L.T("boss_rage")); Fx.Ring(Pos, 1.5f * size, Art.Red); Audio.Play(Audio.Sfx.BossWarn);
            }
            if (rageT > 0) rageT -= dt;

            // 보스: 부하 부르기
            if (!string.IsNullOrEmpty(row.summon) && (row.IsBoss || row.IsElite))
            {
                summonT -= dt;
                if (summonT <= 0 && Battle.I)
                {
                    summonT = Tables.Cf("Boss_Summon_Gap", 4f) * (row.IsElite ? 1.6f : 1f);
                    for (int i = -1; i <= 1; i += 2) Battle.I.Spawn(row.summon, Pos + new Vector2(i * 0.9f, -0.4f), true);
                }
            }

            if (atShield)
            {
                hitT -= dt;
                if (hitT <= 0 && !Paralyzed) { hitT = Tables.Cf("Enemy_Hit_Interval", 1f); Battle.I?.HurtBase(atk, Pos, this); Wobble(); }
                return;
            }
            if (Paralyzed || hold) return;

            // 11-18: 사령탑까지 오는 길이 예전(화면 높이)보다 짧아 Enemy_Speed_Mul 로 맞춤 (밸런스 3차 측정에서 다시)
            float spd = row.speed * Tables.Cf("Enemy_Speed_Mul", 1f) * (1 - slow) * (rageT > 0 ? Tables.Cf("Boss_Rage_Speed", 2.5f) : 1f);
            basePos += dir * spd * dt;
            switch (row.move)
            {
                case "zigzag": sway = Mathf.Sin(age * 2.2f + phase) * 0.9f; break;
                case "hover": sway = Mathf.Sin(age * 0.9f + phase) * 1.8f; break;
                case "boss": sway = Mathf.Sin(age * 0.5f) * 1.2f; break;
                default: sway = 0; break;
            }
            // 달 바닥에 닿으면 바닥을 따라 사령탑 쪽으로 (낮게 오는 적)
            if (basePos.y < World.GroundY + radius * 0.6f) { basePos.y = World.GroundY + radius * 0.6f; if (dir.y < 0) { dir = Vector2.left; perp = Vector2.down; } }
            var p = basePos + perp * sway;
            p.y = Mathf.Max(p.y, World.GroundY + radius * 0.4f);
            pos = p; transform.position = p;
            if (p.x - radius <= World.ShieldX)
            {
                if (row.crash) { Battle.I?.HurtBase(atk * Tables.C("Crash_Mul", 3), Pos, this); Fx.Burst(Pos, 0.6f * size); dead = true; return; }
                atShield = true; hitT = 0.3f;
            }
        }

        float wob; bool hasBar; double barHp = -1;
        bool raged; float rageT;
        void Wobble() { wob = 0.15f; }
        void LateUpdate()
        {
            if (wob > 0) { wob -= Time.deltaTime; transform.localRotation = baseRot * Quaternion.Euler(0, 0, Mathf.Sin(wob * 60) * 8 * wob / 0.15f); }
            if (hasBar && turnToDir) { barBg.transform.rotation = Quaternion.identity; barFill.transform.rotation = Quaternion.identity; }
            if (hasBar && hp != barHp) { barHp = hp; float k = (float)(hp / maxHp); barFill.size = new Vector2(1.6f * Mathf.Clamp01(k), 0.3f); barFill.transform.localPosition = barBg.transform.localPosition + new Vector3(-0.8f * (1 - k) * barK / size, 0, 0); }
        }

        /// <summary>피해 → 죽으면 true</summary>
        public bool Damage(double d)
        {
            if (dead) return false;
            hp -= d;
            flash = 0.1f;
            if (hp <= 0) { hp = 0; dead = true; return true; }
            return false;
        }

        public void AddSlow(float amt, float time, float max)
        {
            slow = Mathf.Min(max, slow + amt);
            slowT = Mathf.Max(slowT, time);
        }
        public void Paralyze(float time)
        {
            if (row.IsBoss) return;      // 보스는 마비 안 됨 (기획서 6-2)
            para = Mathf.Max(para, time);
        }
        public void Burn(double dps, float time, TurretInst src) { burnDps = burnT > 0 ? System.Math.Max(burnDps, dps) : dps; burnT = time; burnSrc = src; }   // 11-17: 큰 수가 float 로 깎이던 것 · 화상이 끝난 뒤에도 예전 큰 값이 남던 것 고침
        public void Vulnerable(double amt, float time) { vuln = System.Math.Max(vuln, amt); vulnT = time; }
        /// <summary>밀어내기 (쾅쾅 대포 · 기뢰 충격파 · 충격 미사일): 온 길로 d 칸 되돌림(11-18: 예전 = 위로). 보스는 안 밀림 · 중간 보스는 30%만</summary>
        public void Knock(float d)
        {
            if (row.IsBoss || d <= 0 || dead) return;
            if (row.IsElite) d *= 0.3f;
            Knocks++;
            var b = basePos - dir * d;
            b.y = Mathf.Min(b.y, World.SpawnY - 0.3f); b.x = Mathf.Min(b.x, World.HalfW - 0.3f);
            basePos = b; pos = b + perp * sway; transform.position = pos;
            if (atShield && pos.x - radius > World.ShieldX + 0.05f) atShield = false;
        }

        /// <summary>끌어당김 (장판 · 소용돌이)</summary>
        public void Pull(Vector2 to, float amt)
        {
            if (row.IsBoss) return;
            var np = Vector2.MoveTowards(pos, to, amt);
            basePos += np - pos; pos = np;
            transform.position = pos;
        }
    }
}
