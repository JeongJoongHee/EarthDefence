using UnityEngine;

namespace EarthDef
{
    /// <summary>
    /// 적 하나. 위에서 내려와 방어막(Shield_Y)에 닿으면: 운석(Crash)은 부딪쳐 터지며 큰 피해, 나머지는 멈춰서 계속 때림.
    /// 움직임(Move): fall 곧게 · zigzag 흔들며 · fast 빠르게 · walk 천천히 · hover 좌우로 크게 · boss 느리게 + 부하 부르기.
    /// 상태이상: 둔화 · 마비 · 화상 · 취약. 맞으면 흰 실루엣이 잠깐 번쩍.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        public EnemyRow row;
        public double hp, maxHp, atk;
        public bool dead;
        public float radius;
        public Vector2 Pos => transform.position;

        SpriteRenderer sr, white;
        SpriteRenderer barBg, barFill;
        float flash, age, x0, phase, hitT, summonT;
        bool atShield;
        // 상태이상
        public float slow, slowT, para, vulnT;
        public double vuln;
        float burnT, burnTick; double burnDps; TurretInst burnSrc;
        public bool Paralyzed => para > 0;

        public void Init(EnemyRow r, Vector2 p, double hp, double atk)
        {
            row = r; this.hp = maxHp = hp; this.atk = atk;
            transform.position = p;
            transform.localScale = Vector3.one * r.size;
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Art.Enemy(r); sr.sortingOrder = r.IsBoss ? 12 : r.IsElite ? 11 : 10;
            var w = new GameObject("White"); w.transform.SetParent(transform, false);
            white = w.AddComponent<SpriteRenderer>(); white.sprite = Art.Enemy(r, true); white.sortingOrder = sr.sortingOrder; white.color = new Color(1, 1, 1, 0);
            // 맞는 크기: 그림 242px(PPU200) = 1.21칸 → 반지름 약 0.42 x Size (보스 그림은 2배)
            radius = (r.IsBoss && r.art == "planet" ? 0.95f : 0.42f) * r.size;
            x0 = p.x; phase = Random.Range(0f, 6.28f);
            summonT = Tables.Cf("Boss_Summon_Gap", 4f);
            if (r.IsBoss || r.IsElite)
            {
                barBg = Bar("BarBg", Art.UI("track"), 20); barFill = Bar("BarFill", Art.UI("fill"), 21);
                barFill.color = Art.Red;
            }
            SetAlpha(0);
        }

        SpriteRenderer Bar(string name, Sprite s, int order)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var b = go.AddComponent<SpriteRenderer>();
            b.sprite = s; b.drawMode = SpriteDrawMode.Sliced; b.sortingOrder = order;
            float top = (row.art == "planet" ? 1.25f : 0.75f);
            go.transform.localPosition = new Vector3(0, top, 0);
            go.transform.localScale = Vector3.one / row.size;
            b.size = new Vector2(1.6f, 0.3f);
            return b;
        }

        void SetAlpha(float a) { var c = sr.color; c.a = a; sr.color = c; }

        public void Step(float dt)
        {
            if (dead) return;
            age += dt;
            if (age < 0.4f) SetAlpha(age / 0.4f); else if (sr.color.a < 1) SetAlpha(1);
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
                if (hitT <= 0 && !Paralyzed) { hitT = Tables.Cf("Enemy_Hit_Interval", 1f); Battle.I?.HurtBase(atk, Pos); Wobble(); }
                return;
            }
            if (Paralyzed) return;

            float spd = row.speed * (1 - slow);
            var p = transform.position;
            switch (row.move)
            {
                case "zigzag": p.y -= spd * dt; p.x = x0 + Mathf.Sin(age * 2.2f + phase) * 0.9f; break;
                case "hover": p.y -= spd * dt; p.x = x0 + Mathf.Sin(age * 0.9f + phase) * 1.8f; break;
                case "boss": p.y -= spd * dt; p.x = x0 + Mathf.Sin(age * 0.5f) * 1.2f; break;
                default: p.y -= spd * dt; break;
            }
            p.x = Mathf.Clamp(p.x, -World.HalfW + radius, World.HalfW - radius);
            transform.position = p;
            if (p.y - radius <= World.ShieldY)
            {
                if (row.crash) { Battle.I?.HurtBase(atk * Tables.C("Crash_Mul", 3), Pos); Fx.Burst(Pos, 0.6f * row.size); dead = true; return; }
                atShield = true; hitT = 0.3f;
            }
        }

        float wob;
        void Wobble() { wob = 0.15f; }
        void LateUpdate()
        {
            if (wob > 0) { wob -= Time.deltaTime; transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(wob * 60) * 8 * wob / 0.15f); }
            if (barFill) { float k = (float)(hp / maxHp); barFill.size = new Vector2(1.6f * Mathf.Clamp01(k), 0.3f); barFill.transform.localPosition = barBg.transform.localPosition + new Vector3(-0.8f * (1 - k) / row.size, 0, 0); }
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
        public void Burn(double dps, float time, TurretInst src) { burnDps = Mathf.Max((float)burnDps, (float)dps); burnT = time; burnSrc = src; }
        public void Vulnerable(double amt, float time) { vuln = System.Math.Max(vuln, amt); vulnT = time; }
        /// <summary>끌어당김 (장판 · 소용돌이)</summary>
        public void Pull(Vector2 to, float amt)
        {
            if (row.IsBoss) return;
            var p = (Vector2)transform.position;
            transform.position = Vector2.MoveTowards(p, to, amt);
            x0 = transform.position.x;
        }
    }
}
