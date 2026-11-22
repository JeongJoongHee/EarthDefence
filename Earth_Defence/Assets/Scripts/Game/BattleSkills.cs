using UnityEngine;

namespace EarthDef
{
    /// <summary>
    /// 11-21 요청 "대장 카피의 특수 능력 // 돔 특수 능력을 발동시킬 수 있는 버튼 (예: 대장 카피 버닝 // 돔의 적 전체 얼림 // 적 전체 감소)"
    ///  · 대장 카피 버닝: Burn_Time 초 동안 대장 카피 공격 속도 x Burn_Rate_Mul · 피해 x Burn_Dmg_Mul — 쿨타임 Burn_Cd
    ///  · 돔 얼음 파동: 화면의 적 모두 Dome_Freeze_Time 초 얼림(보스는 얼지 않는 규칙 → Dome_Boss_Slow 만큼 느려짐) + 체력 Dome_Dmg_Pct% 감소 (엘리트 · 보스는 Dome_Dmg_Boss_Pct%) — 쿨타임 Dome_Skill_Cd
    ///  · 전투 시작 Skill_First_Wait 초 뒤부터 쓸 수 있음 · Config Skill_On 0 = 단추 숨김
    /// 단추 = BattleUI (아래 왼쪽 버닝 · 오른쪽 얼음 파동, 남은 쿨타임 원형 게이지)
    /// </summary>
    public partial class Battle
    {
        public float burnT, skillCdG, skillCdD;
        public int burnUsed, domeUsed;
        float burnFx;

        public static bool SkillOn => Tables.Ci("Skill_On", 1) == 1;
        public float BurnCdMax => Tables.Cf("Burn_Cd", 45f);
        public float DomeCdMax => Tables.Cf("Dome_Skill_Cd", 60f);
        public bool Burning => burnT > 0;
        public bool CanBurn => SkillOn && st == St.Play && skillCdG <= 0 && Guardian != null && lab == null;
        public bool CanDome => SkillOn && st == St.Play && skillCdD <= 0 && lab == null && !DomeSpent;
        /// <summary>11-22 의견 "1회성": 한 판에 Dome_Skill_Uses 번 쓰면 끝 (0 = 쿨타임마다)</summary>
        public bool DomeSpent => Tables.Ci("Dome_Skill_Uses", 1) > 0 && domeUsed >= Tables.Ci("Dome_Skill_Uses", 1);
        /// <summary>버닝 중 대장 카피 공격 간격에 곱함 (TurretUnit)</summary>
        public float BurnCdMul => Burning ? 1f / Mathf.Max(0.1f, Tables.Cf("Burn_Rate_Mul", 2f)) : 1f;
        /// <summary>버닝 중 대장 카피 피해에 곱함 (Combat.Calc)</summary>
        public double BurnDmgMul => Burning ? Tables.C("Burn_Dmg_Mul", 1.5) : 1;

        void SkillStart()
        {
            burnT = 0; burnUsed = domeUsed = 0;
            skillCdG = skillCdD = Tables.Cf("Skill_First_Wait", 10f);
        }

        public bool UseBurn()
        {
            if (!CanBurn) return false;
            burnT = Tables.Cf("Burn_Time", 8f);
            skillCdG = BurnCdMax;
            burnUsed++;
            var p = (Vector2)Guardian.transform.position + Vector2.up * 0.9f;
            Fx.Ring(p, 1.4f, Art.Hex("#ffb347"));
            Fx.Sparks(p, Art.Hex("#ffb347"), 14, 6f, 1.2f);
            Audio.Play(Audio.Sfx.Upgrade);
            Changed?.Invoke();
            return true;
        }

        public bool UseDome()
        {
            if (!CanDome) return false;
            skillCdD = DomeCdMax;
            domeUsed++;
            float freeze = Tables.Cf("Dome_Freeze_Time", 3f);
            double pct = Tables.C("Dome_Dmg_Pct", 10) / 100.0, pctBoss = Tables.C("Dome_Dmg_Boss_Pct", 3) / 100.0;
            var src = run.turrets.Count > 0 ? run.turrets[0] : null;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (i >= enemies.Count) continue;
                var e = enemies[i];
                if (e.dead || !World.OnField(e.Pos)) continue;
                // 11-22 의견 "빙결 2초간 모두 이동 정지 + 받는 피해 증가": Dome_Freeze_Boss 1 = 보스도 얼림 (0 = 예전처럼 보스는 느려짐)
                if (e.row.IsBoss && Tables.Ci("Dome_Freeze_Boss", 1) != 1) { float sl = Tables.Cf("Dome_Boss_Slow", 0.5f); e.AddSlow(sl, freeze, sl); }
                else e.Paralyze(freeze, true);
                if (Tables.C("Dome_Vuln", 30) > 0) e.Vulnerable(Tables.C("Dome_Vuln", 30) / 100.0, freeze);
                Fx.Ring(e.Pos, 0.6f, Art.Hex("#9fe8ff"));
                if (src != null) Combat.Raw(e, e.maxHp * (e.row.IsBoss || e.row.IsElite ? pctBoss : pct), false, src, true);
            }
            Fx.Ring(World.BaseCenter, 6f, Art.Hex("#9fe8ff"));
            Fx.Ring(World.BaseCenter, 3.5f, Color.white);
            Audio.Play(Audio.Sfx.Zap);
            Flush();
            Changed?.Invoke();
            return true;
        }

        void SkillStep(float dt)
        {
            BuffStep(dt);       // 11-22 별빛 증폭탑 버프 시간
            if (skillCdG > 0) skillCdG -= dt;
            if (skillCdD > 0) skillCdD -= dt;
            if (burnT <= 0) return;
            burnT -= dt;
            if ((burnFx -= dt) <= 0 && Guardian)
            {
                burnFx = 0.12f;
                var p = (Vector2)Guardian.transform.position + Vector2.up * 0.8f;
                Fx.Glow(p, 1.1f, new Color(1f, 0.55f, 0.2f, 0.45f), 0.18f);
                Fx.Sparks(p, Art.Hex("#ffb347"), 2, 3f, 0.9f, 70f, 90f);
            }
        }
    }
}
