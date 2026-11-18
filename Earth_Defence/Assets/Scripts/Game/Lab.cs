using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EarthDef
{
    /// <summary>
    /// 터렛 시험장 (11-16 밤 사용자 "터렛을 테스트할 수 있는 별도의 테스트 씬 — 원하는 터렛을 끌어다 놓고 발사체와 속도를 확인").
    /// 씬 Assets/Scenes/TurretLab.unity (메뉴 지구 방위대 › 터렛 시험장 열기) = 카메라 + LabBoot.
    /// 규칙: 기지는 안 닳음 · 레벨업 없음 · 끝나지 않음 · 세이브에 쓰지 않음(공격력 · 터렛 레벨 · 장비는 지금 세이브 그대로 읽기만).
    /// 적: 무리(스테이지 적이 계속) · 허수아비(제자리 3줄, 체력 아주 많음) · 보스(체력 아주 많은 보스 하나) · 없음.
    /// </summary>
    public class Lab
    {
        public string mode = "dummy";
        public int stage = 10;
        public bool guardianFire;
        float acc, check;
        readonly List<Enemy> mine = new();

        public static readonly string[] Presets = { "base", "A", "B", "all" };

        /// <summary>카드 상태: base = 카드 없음 · A/B = 그 줄 돌파 + 연계 + 강화 최대 · all = 두 줄 다(3단계)</summary>
        public static void ApplyPreset(TurretInst t, string preset)
        {
            t.picks.Clear(); t.tier = 1; t.stars = 1;
            if (preset == "base") { t.Recalc(); return; }
            foreach (var c in Tables.Cards)
            {
                if (c.turret != t.row.id) continue;
                bool line = preset == "all" || c.line == preset;
                if (c.kind == "normal") t.picks[c.id] = c.max;
                else if ((c.kind == "break" || c.kind == "chain") && line) t.picks[c.id] = 1;
            }
            t.tier = preset == "all" ? 3 : 2;
            t.stars = 0;
            t.Recalc();
        }

        public void SetMode(Battle b, string m)
        {
            mode = m;
            foreach (var e in b.enemies) e.dead = true;   // 다음 Flush 에 지워짐
            mine.Clear(); acc = 0; check = 0;
        }

        public void Spawn(Battle b, float dt)
        {
            switch (mode)
            {
                case "swarm":
                    acc += 2.2f * dt;
                    while (acc >= 1 && b.enemies.Count < 70)
                    {
                        acc -= 1;
                        var st = Tables.Stage(stage);
                        b.Spawn(st.enemies[Random.Range(0, st.enemies.Length)], b.SpawnPoint());
                    }
                    break;
                case "dummy":
                    if ((check -= dt) > 0) break;
                    check = 0.5f;
                    mine.RemoveAll(e => e == null || e.dead);
                    if (mine.Count >= 15) break;
                    // 3줄 x 5 — 빈 자리를 채움
                    for (int r = 0; r < 3; r++)
                        for (int c = 0; c < 5; c++)
                        {
                            var at = new Vector2(-3.6f + c * 1.8f, -1.6f + r * 2.2f);
                            if (mine.Exists(e => Vector2.Distance(e.Pos, at) < 0.3f)) continue;
                            var e2 = b.Spawn(r == 2 ? "jelly" : r == 1 ? "beetle" : "moth", at);
                            e2.hold = true; e2.hp = e2.maxHp = 1e12; mine.Add(e2);
                        }
                    break;
                case "boss":
                    if ((check -= dt) > 0) break;
                    check = 1f;
                    mine.RemoveAll(e => e == null || e.dead);
                    if (mine.Count == 0)
                    {
                        var e3 = b.Spawn(Tables.Stage(stage).boss, new Vector2(0, 3.2f));
                        e3.hold = true; e3.hp = e3.maxHp = 1e14; mine.Add(e3);
                    }
                    break;
            }
        }
    }
}
