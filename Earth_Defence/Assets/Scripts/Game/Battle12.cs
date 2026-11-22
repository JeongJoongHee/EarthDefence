using UnityEngine;

namespace EarthDef
{
    /// <summary>
    /// 11-22 별빛 증폭탑 (타워_전체_정리 #18): 쏠 때마다 Duration 초 동안 놓인 아군 타워 모두(대장 카피 포함)에 버프.
    ///  · 피해 = Rate(25%) x (1 + 터렛 레벨 피해 보너스 x Prism_Lv_Mul) + 카드 · 회로 buff_dmg  → Combat.Calc 일반 브래킷
    ///  · 치명 확률 buff_crit · 치명 피해 buff_crit_dmg · 공속 buff_cd(초신성 과부하) · 보스 특공 buff_boss · 대장 카피 guardian_bonus
    ///  · 약점 파동(aura_vuln · aura_slow): 버프 때 화면의 적 모두 취약 + 표식(markUntil — 표식 파열)
    ///  · 군집 지휘(buff_cdr_burst): 버프 순간 아군 남은 쿨다운 -15%
    ///  · 필살기 은하 과부하: 4초 공속 +Ult_Rate% · 치명 +30%
    /// </summary>
    public partial class Battle
    {
        public float buffT, overT, buffFxT;
        public double buffDmg, buffCrit, buffCritDmg, buffCd, buffBoss, buffGuardian, overCd, overCrit;
        public int buffPulses;   // 시험용
        public bool Buffing => buffT > 0;
        /// <summary>쿨다운에 더하는 공속 % (TurretInst.Cooldown)</summary>
        public double BuffCdPct => (buffT > 0 ? buffCd : 0) + (overT > 0 ? overCd : 0);
        public double BuffCrit => (buffT > 0 ? buffCrit : 0) + (overT > 0 ? overCrit : 0);

        void BuffReset() { buffT = overT = 0; buffDmg = buffCrit = buffCritDmg = buffCd = buffBoss = buffGuardian = overCd = overCrit = 0; buffPulses = 0; }

        public void PrismPulse(TurretInst t)
        {
            buffPulses++;
            buffT = t.Duration;
            buffDmg = t.row.rate / 100.0 * (1 + t.Lv.dmgBonus * Tables.C("Prism_Lv_Mul", 0.4)) + t.S("buff_dmg") / 100.0;
            buffCrit = t.S("buff_crit") / 100.0;
            buffCritDmg = t.S("buff_crit_dmg") / 100.0;
            buffCd = t.S("buff_cd");
            buffBoss = t.S("buff_boss") / 100.0;
            buffGuardian = t.S("guardian_bonus") / 100.0;
            var c = Art.Hex("#d4b8ff");
            var head = t.unit ? t.unit.Head : World.BaseCenter;
            Fx.Ring(head, 2.4f, c); Fx.Glow(head, 1.4f, new Color(c.r, c.g, c.b, 0.8f), 0.35f);
            Fx.Word(head + Vector2.up * 0.6f, L.T("prism_buff"), Color.Lerp(c, Color.white, 0.3f));
            double burst = t.S("buff_cdr_burst") / 100.0;
            foreach (var u in units)
            {
                if (!u || u.t == t) continue;
                Fx.Ring((Vector2)u.transform.position + Vector2.up * 0.6f, 1.0f, c);
                if (burst > 0) u.CutCd(burst);
            }
            if (t.Has("aura_vuln"))
            {
                double v = t.S("aura_vuln") / 100.0; float sl = (float)t.S("aura_slow") / 100f;
                foreach (var e in enemies)
                {
                    if (e.dead || !World.OnField(e.Pos)) continue;
                    e.Vulnerable(v, buffT); e.markUntil = time + buffT;
                    if (sl > 0) e.AddSlow(sl, buffT, Tables.Cf("Slow_Max", 0.5f));
                }
                Fx.Ring(World.FieldCenter, 6f, c);
            }
            Audio.Play(Audio.Sfx.Upgrade, 0.7f);
        }

        /// <summary>필살기 은하 과부하</summary>
        public void PrismOverload(TurretInst t, double cdPct)
        {
            overT = 4f; overCd = cdPct; overCrit = 0.3;
            foreach (var u in units) if (u) Fx.Ring((Vector2)u.transform.position + Vector2.up * 0.6f, 1.4f, Art.Hex("#ffe888"));
        }

        void BuffStep(float dt)
        {
            if (overT > 0) overT -= dt;
            if (buffT <= 0) return;
            buffT -= dt;
            if ((buffFxT -= dt) > 0) return;
            buffFxT = 0.5f;
            var c = new Color(0.83f, 0.72f, 1f, 0.45f);
            foreach (var u in units) if (u && u.t.row.kind != "prism") Fx.Glow((Vector2)u.transform.position + Vector2.up * 0.6f, 0.8f, c, 0.25f);
        }
    }
}
