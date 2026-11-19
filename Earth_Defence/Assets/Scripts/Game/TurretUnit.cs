using UnityEngine;

namespace EarthDef
{
    /// <summary>
    /// 전장의 터렛 1개 (가디언 포함). 쿨다운이 차면 종류(Kind)대로 쏨:
    /// gun 가디언 연사 · rail 관통 포탄 · laser 레이저 · field 가로 장판 · hive 벌 · missile 유도 미사일 · bolt 세로 낙뢰.
    /// 단계가 오르면 그림이 조금 커짐(1 → 1.1 → 1.2).
    /// </summary>
    public class TurretUnit : MonoBehaviour
    {
        public TurretInst t;
        SpriteRenderer sr;
        float cd, chargeT, chargeShotT, kick;
        public double charge;             // 가디언 충전 포인트
        public bool Charging => chargeT > 0;
        public float ChargeK => Charging ? chargeT / Tables.Cf("Charge_Time", 5f) : (float)(charge / Tables.C("Charge_Need", 120));
        public float targetX;
        int muzzle;
        public int shotsFired, fired, extraShots;
        bool IsGuardian => t.row.kind == "gun";
        float baseY, baseScale = 1f;

        public void Init(TurretInst inst, Vector2 pos)
        {
            t = inst; inst.unit = this;
            transform.position = pos; baseY = pos.y; targetX = pos.x;
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = IsGuardian ? Art.Guardian : Art.Turret(t.row.art);
            sr.sortingOrder = IsGuardian ? 5 : 3;
            cd = IsGuardian ? 0.3f : 0.6f + t.slot * 0.35f;
            if (IsGuardian) MakeGun();
            else SpawnFx();
            // 11-16 밤 대기 효과 — 받침 그림 + 움직이는 조각 (터렛 6종 모두)
            if (!IsGuardian && TurretIdle.Has(t.row.art) && Tables.Cf("Idle_Fx", 1f) > 0)
            {
                sr.sprite = Art.Turret(TurretIdle.BaseSprite(t.row.art));
                idle = gameObject.AddComponent<TurretIdle>();
                idle.Setup(this, t.row.art);
                headDeg = Tables.Cf("Head_Rest_Deg", 45f); idle.RestAim();
            }
            RefreshTier();
        }

        // ---------- 3차: 스킨 (색 돌리기) · 필살기 ----------
        public int ult;                    // 필살기 게이지 (쏠 때마다 +1)
        public int ults;                   // 필살기 쓴 횟수 (시험용)
        public float UltK => t.skin == null ? 0 : (float)ult / t.skin.ultCharge;

        /// <summary>터렛 색: 스킨 = 색 돌리기 · 돌파(2단계 이상) = 붉게 (2차 수정안 "돌파하면 붉은색" — 스킨보다 먼저)</summary>
        public bool Broken => t.tier >= 2 && !IsGuardian;
        public Color LookColor
        {
            get
            {
                var c = Elem.Color(t.row.elem);
                if (Broken) { Color.RGBToHSV(c, out _, out var s, out var v); return Color.HSVToRGB(Tables.Cf("Break_Hue", 0f), Mathf.Clamp01(Mathf.Max(0.45f, s) * Tables.Cf("Break_Sat", 1.25f)), v); }
                return t.skin != null ? Art.HueShift(c, t.skin.hue, t.skin.sat) : c;
            }
        }
        readonly System.Collections.Generic.Dictionary<SpriteRenderer, Color> addBase = new();
        bool looked;

        void ApplyLook()
        {
            if (IsGuardian || (t.skin == null && !Broken && !looked)) return;   // 한 번도 안 바꿨으면 그대로 (시험장에서 돌파 → 기본으로 돌아오면 원래 색으로)
            looked = true;
            var mat = Art.HueMat; var mpb = new MaterialPropertyBlock();
            float sat = Broken ? Tables.Cf("Break_Sat", 1.25f) * (t.tier >= 3 ? 1.2f : 1f) : t.skin != null ? t.skin.sat : 1f;
            foreach (var r in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (r.sharedMaterial == Art.AddMat)
                {
                    if (!addBase.ContainsKey(r)) addBase[r] = r.color;
                    var c0 = addBase[r];
                    if (Broken) { Color.RGBToHSV(c0, out _, out var s0, out var v0); var c1 = Color.HSVToRGB(Tables.Cf("Break_Hue", 0f), Mathf.Clamp01(s0 * sat), v0); c1.a = c0.a; r.color = c1; }
                    else r.color = t.skin != null ? Art.HueShift(c0, t.skin.hue, t.skin.sat) : c0;
                    continue;
                }
                if (!mat) continue;
                r.sharedMaterial = mat;
                r.GetPropertyBlock(mpb);
                mpb.SetFloat("_Hue", t.skin != null ? t.skin.hue / 360f : 0f); mpb.SetFloat("_Sat", sat);
                mpb.SetFloat("_SetHue", Broken ? Tables.Cf("Break_Hue", 0f) : -1f);
                r.SetPropertyBlock(mpb);
                if (r == sr || r.transform.parent == transform) r.color = Broken ? (t.tier >= 3 ? new Color(1f, 0.8f, 0.78f) : new Color(1f, 0.88f, 0.86f)) : Color.white;   // 회색 쇠 부분도 살짝 붉게
            }
            if (idle) idle.Tint(LookColor);
        }

        void UltStep(Enemy target)
        {
            if (t.skin == null || Tables.Ci("Ult_On", 1) != 1) return;
            if (++ult < t.skin.ultCharge) return;
            ult = 0; ults++;
            CastUlt(target);
        }

        /// <summary>필살기: 터렛마다 큰 공격 (Skin.xlsx Ult_Rate %ATK)</summary>
        void CastUlt(Enemy target)
        {
            var b = Battle.I; var k = t.skin; double rate = k.ultRate;
            var col = LookColor;
            Fx.Word(Head + Vector2.up * 0.6f, k.ultName, Color.Lerp(col, Color.white, 0.3f));
            Fx.Ring(Head, 2.2f, col); Fx.Glow(Head, 1.2f, new Color(col.r, col.g, col.b, 0.8f), 0.4f);
            Audio.Play(Audio.Sfx.Break);
            var tp = target != null && !target.dead ? target.Pos : World.FieldCenter;
            float ang = Mathf.Atan2(tp.y - Head.y, tp.x - Head.x) * Mathf.Rad2Deg;
            switch (k.ult)
            {
                case "rail": b.Add(new Beam(t, Head, ang, 0.8f, 0.1f, 0.9f, rate / 8)); break;
                case "laser": for (int i = -1; i <= 1; i++) b.Add(new Beam(t, Head, ang + i * 25f, 1.5f, 0.2f, 0.4f, rate, 40f)); break;
                case "drone":
                {
                    if (Tables.Ci("Field_Vertical", 1) == 1)
                    {   // 8차: 세로 띠 3개 (적 무리 x 와 양옆)
                        float hw = 0.9f * t.Area, x = Mathf.Clamp(b.CrowdX(), World.ShieldX + hw + 2.4f, World.HalfW - hw - 2.4f);
                        for (int i = -1; i <= 1; i++) b.Add(new Field(t, x + i * 2.4f, hw, 4f, 0.4f, rate));
                        break;
                    }
                    float y = b.CrowdY();
                    for (int i = -1; i <= 1; i++) b.Add(new Field(t, Mathf.Clamp(y + i * 2.4f, World.GroundY + 1.2f, 8f), 0.9f * t.Area, 4f, 0.4f, rate));
                    break;
                }
                case "hive": for (int i = 0; i < 15; i++) b.Add(new Bee(t, Head + new Vector2(Random.Range(-0.6f, 0.6f), Random.Range(0f, 0.4f)), 5, rate)); break;
                case "missile":
                    for (int i = 0; i < 12; i++)
                    {
                        var from = new Vector2(Random.Range(World.ShieldX + 0.5f, World.HalfW - 0.5f), World.SpawnY - 0.2f);
                        var m = new Projectile(t, "missile", from, Vector2.down, t.row.speed, rate, 0, 1f) { target = b.RandomEnemy(), turn = 5f, life = 5f, hitR = 0.25f };
                        m.onHit = (p, e) => { Combat.Hit(t, e, rate); Combat.Explode(t, p.pos, 1.1f, rate * 0.6, Elem.Color("fire")); };
                        b.Add(m);
                    }
                    break;
                case "bolt": for (int i = 0; i < 7; i++) b.Add(new Strike(t, Mathf.Lerp(World.ShieldX + 0.5f, World.HalfW - 0.4f, i / 6f), 0.7f, 1.2f, 0.3f, rate)); break;
                case "cannon":
                {
                    var d = (tp - Head).normalized;
                    var pr = new Projectile(t, "cannonball", Head, d, t.row.speed, rate, 0, 4f) { hitR = 0.8f, life = 3f };
                    pr.onHit = (p, e) =>
                    {
                        Combat.Explode(t, p.pos, 3f, rate, col);
                        foreach (var en in b.enemies) if (!en.dead && Vector2.Distance(en.Pos, p.pos) < 3.2f) en.Knock(Tables.Cf("Knock_Base", 0.6f) * 2.5f);
                    };
                    b.Add(pr);
                    break;
                }
                case "focus":
                {
                    var skip = new System.Collections.Generic.HashSet<Enemy>();
                    for (int i = 0; i < 3; i++) { var e = b.Nearest(Head, t.Range * 1.5f, skip); if (e == null) break; skip.Add(e); b.Add(new FocusBeam(t, e, 5f, t.row.radius * 1.5f)); }
                    break;
                }
                case "vortex": b.Add(new Vortex(t, World.FieldCenter, Vector2.up, 0, 3f, 2.4f, rate, 7f, true)); break;
                case "wheel":
                    for (int i = 0; i < 6; i++)
                    {
                        float a = 30f + i * 24f;
                        b.Add(new FireWheel(t, Head, new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * t.row.speed, t.row.radius * t.Area, t.row.hits, t.Duration, t.row.rate));
                    }
                    break;
                case "tesla": for (int i = 0; i < 3; i++) b.Add(new TeslaBolt(t, i == 0 ? target : b.RandomEnemy(), Head, 0.3f + i * 0.15f, 12, t.row.rate, t.row.radius * t.Area)); break;
            }
        }

        // ---------- 11-16 저녁: 생길 때 톡 커짐 · 빛 · 불꽃 (사용자 "터렛이 생성된 느낌이 필요") ----------
        // 터렛 아래 속성 색 빛 고리는 11-16 밤에 뺌 (사용자 "대기 효과를 넣었으니 하단 강조 효과는 제거")
        public TurretIdle idle;
        /// <summary>다음 발사까지 찬 정도 0~1 (대기 효과가 쏠 때가 가까울수록 세짐)</summary>
        public float Charge01 => 1f - Mathf.Clamp01(cd / Mathf.Max(0.05f, t.Cooldown));
        float popT = -1f;

        void SpawnFx()
        {
            popT = 0f;
            var c = Elem.Color(t.row.elem);
            Fx.Ring((Vector2)transform.position + Vector2.up * 0.5f, 1.1f, c);
            Fx.Glow((Vector2)transform.position + Vector2.up * 0.6f, 0.9f, new Color(c.r, c.g, c.b, 0.8f), 0.4f);
            Fx.Sparks((Vector2)transform.position + Vector2.up * 0.5f, Color.Lerp(c, Color.white, 0.4f), 12, 4.5f, 1.1f, 160f, 90f);
        }

        void Update()
        {
            if (popT < 0f) return;
            popT += Time.deltaTime;
            float k = Mathf.Clamp01(popT / 0.4f);
            float s = k < 1 ? 1 + 2.2f * Mathf.Pow(k - 1, 3) + 1.2f * Mathf.Pow(k - 1, 2) : 1;   // 뒤로 살짝 넘치며 커짐
            transform.localScale = Vector3.one * baseScale * Mathf.Max(0.05f, s);
            if (k >= 1) { popT = -1f; transform.localScale = Vector3.one * baseScale; }
        }

        // ---------- 11-16 오후: 가디언 = 카치건 형태 몸 + 따로 도는 레이저 소총 ----------
        // 총은 뒤 손(Art.GuardianHand)을 축으로 목표 쪽으로 돎 · 목표가 왼쪽이면 몸을 뒤집음 · 쏠 때는 총만 뒤로 밀림(몸은 그대로)
        // 이동은 부드럽게 가속·감속(SmoothDamp) + 가는 쪽으로 살짝 기울고 뒤뚱 (사용자 "방방 거림 · 무게감 없이 가벼움")
        Transform gunRoot, gunT;
        float gunAng = 50f, aimAng = 50f, recoil, moveVel, walkPh, aimT, moveLean, backLean;
        bool faceLeft;
        public bool FaceLeft => faceLeft;
        public float GunAngle => gunAng;

        void MakeGun()
        {
            gunRoot = new GameObject("GunRoot").transform;
            gunRoot.SetParent(transform, false);
            gunT = new GameObject("Gun").transform;
            gunT.SetParent(gunRoot, false);
            var gs = gunT.gameObject.AddComponent<SpriteRenderer>();
            gs.sprite = Art.GuardianGun; gs.sortingOrder = 6;
            PoseGun();
        }

        Vector2 HandPos => transform.TransformPoint(new Vector3(Art.GuardianHand.x * (faceLeft ? -1 : 1), Art.GuardianHand.y, 0));

        void PoseGun()
        {
            if (!gunRoot) return;
            gunRoot.localPosition = new Vector3(Art.GuardianHand.x * (faceLeft ? -1 : 1), Art.GuardianHand.y, 0);
            gunRoot.localScale = new Vector3(faceLeft ? -1 : 1, 1, 1);
            float lean = transform.localEulerAngles.z;
            gunRoot.localRotation = Quaternion.Euler(0, 0, (faceLeft ? gunAng - 180f : gunAng) - lean);
            gunT.localPosition = new Vector3(-recoil, 0, 0);
        }

        void AimStep(float dt)
        {
            if ((aimT -= dt) <= 0)
            {
                aimT = 0.1f;
                var tg = Battle.I.Nearest(HandPos, t.Range);
                if (tg != null)
                {
                    var d = tg.Pos - HandPos;
                    aimAng = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                    if (aimAng < -90f) aimAng += 360f;
                }
            }
            // 몸 방향: 겨눌 각이 90°를 넘으면 왼쪽 (흔들리지 않게 ±12° 여유)
            if (!faceLeft && aimAng > 102f) { faceLeft = true; sr.flipX = true; gunAng = 180f - gunAng; }
            else if (faceLeft && aimAng < 78f) { faceLeft = false; sr.flipX = false; gunAng = 180f - gunAng; }
            // 높이 겨눌수록 몸을 뒤로 젖힘(최대 14°) · 총은 몸에 대해 62°까지만 듦 → 수직으로 겨눠도 총이 얼굴(주둥이)을 안 가림
            float elevWant = faceLeft ? 180f - aimAng : aimAng;
            backLean = Mathf.MoveTowards(backLean, Mathf.Clamp((elevWant - 45f) * 0.4f, 0f, 14f), 60f * dt);
            float elev = Mathf.Clamp(elevWant, -15f, 62f + backLean);
            gunAng = Mathf.MoveTowardsAngle(gunAng, faceLeft ? 180f - elev : elev, 720f * dt);
            float lean = moveLean + (faceLeft ? -backLean : backLean);
            transform.localRotation = Quaternion.Euler(0, 0, Mathf.LerpAngle(transform.localEulerAngles.z, lean, Mathf.Min(1, 10f * dt)));
            recoil = Mathf.MoveTowards(recoil, 0, 0.5f * dt) * Mathf.Max(0, 1 - 12f * dt);
            PoseGun();
        }

        public void RefreshTier()
        {
            float s = 1 + 0.1f * (t.tier - 1);
            baseScale = s;
            transform.localScale = Vector3.one * s;
            ApplyLook();
            if (t.tier > 1 || t.skin != null) Fx.Ring((Vector2)transform.position + Vector2.up * 0.8f, 1.2f, LookColor);
        }

        public Vector2 Head => IsGuardian ? (Vector2)gunT.TransformPoint(Art.GunMuzzle)
                                   : idle != null && idle.HasHead ? idle.TipWorld
                                   : (Vector2)transform.position + Vector2.up * Art.TurretHead * transform.localScale.x;
        // ---------- 11-18 사령탑: 머리 45° (쉴 때) → 적 쪽 (사거리 안에 적이 있으면) ----------
        float headDeg = 90f, headWant = 90f, headT;
        public float HeadDeg => headDeg;
        /// <summary>머리가 보는 방향 (머리가 없는 터렛은 위)</summary>
        public Vector2 HeadDir => idle != null && idle.HasHead ? new Vector2(Mathf.Cos(headDeg * Mathf.Deg2Rad), Mathf.Sin(headDeg * Mathf.Deg2Rad)) : Vector2.up;
        float AngleTo(Vector2 p) { var d = p - (Vector2)transform.position - Vector2.up * 0.7f; return Mathf.Clamp(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, -35f, 115f); }
        void HeadStep(float dt)
        {
            if (idle == null || !idle.HasHead) return;
            if ((headT -= dt) <= 0)
            {
                headT = 0.1f;
                var tg = Battle.I.Nearest(transform.position, t.Range);
                headWant = tg != null ? AngleTo(tg.Pos) : Tables.Cf("Head_Rest_Deg", 45f);
            }
            headDeg = Mathf.MoveTowardsAngle(headDeg, headWant, Tables.Cf("Head_Turn", 240f) * dt);
            idle.SetAim(headDeg);
        }

        public void Step(float dt)
        {
            if (!IsGuardian && kick > 0) { kick -= dt; transform.position = new Vector3(transform.position.x, baseY - 0.06f * kick / 0.1f, 0); }
            if (IsGuardian) { if (Tables.Ci("Guardian_Fixed", 1) != 1) Move(dt); AimStep(dt); }   // 11-18: 대장 카피는 사령실 가운데 고정
            else HeadStep(dt);
            if (IsGuardian && Battle.I.lab != null && !Battle.I.lab.guardianFire) return;   // 시험장: 대장 카피 쏘기 끔
            if (Charging) { ChargeStep(dt); return; }
            cd -= dt;
            if (cd > 0) return;
            var b = Battle.I;
            var target = b.Nearest(IsGuardian ? HandPos : Head, t.Range);
            if (target == null) { cd = 0.1f; return; }
            cd = t.Cooldown;
            if (idle) { idle.OnFire(); if (idle.HasHead) { headDeg = headWant = AngleTo(target.Pos); idle.SetAim(headDeg); } }   // 쏠 때는 머리가 적을 봄
            Fire(target);
            fired++;
            UltStep(target);
            kick = 0.1f;
            // 2차 칩 "N% 확률로 한 번 더 쏨" (대장 카피 빼고)
            double ex = b.lo != null ? System.Math.Min(Tables.C("Extra_Shot_Max", 0.5), b.lo.B("extra_shot") / 100.0) : 0;
            if (!IsGuardian && ex > 0 && Combat.R < ex && target != null && !target.dead) { Fire(target); extraShots++; }
        }

        void Move(float dt)
        {
            float max = Tables.Cf("Guardian_X_Max", 1.5f);
            var p = transform.position;
            p.x = Mathf.SmoothDamp(p.x, Mathf.Clamp(targetX, -max, max), ref moveVel, Tables.Cf("Guardian_Smooth", 0.22f), Tables.Cf("Guardian_Move", 14f), dt);
            float sp = Mathf.Abs(moveVel);
            if (sp > 0.25f) walkPh += sp * dt * 2.2f; else walkPh = Mathf.MoveTowards(walkPh, Mathf.Round(walkPh / Mathf.PI) * Mathf.PI, dt * 4f);
            p.y = baseY + Mathf.Abs(Mathf.Sin(walkPh)) * Mathf.Min(0.06f, sp * 0.02f);          // 뒤뚱
            transform.position = p;
            moveLean = Mathf.Clamp(-moveVel * 1.6f, -7f, 7f);                                     // 가는 쪽으로 기움 (AimStep 에서 젖힘과 합쳐 돌림)
        }

        void Fire(Enemy target)
        {
            var b = Battle.I;
            Audio.Play(t.row.kind switch
            {
                "rail" => Audio.Sfx.Rail, "laser" => Audio.Sfx.Laser, "field" => Audio.Sfx.Field, "hive" => Audio.Sfx.Bee, "missile" => Audio.Sfx.Missile,
                "bolt" => Audio.Sfx.Bolt, "cannon" => Audio.Sfx.BigBoom, "focus" => Audio.Sfx.Focus, "vortex" => Audio.Sfx.Vortex, "wheel" => Audio.Sfx.Wheel,
                "tesla" => Audio.Sfx.Tesla, _ => Audio.Sfx.Gun,
            });
            switch (t.row.kind)
            {
                case "gun": Gun(target, 1, t.row.rate); break;
                case "rail":
                    for (int i = 0; i < t.Shots; i++)
                    {
                        var d = Rot((target.Pos - Head).normalized, (i - (t.Shots - 1) / 2f) * 6f);
                        var pr = new Projectile(t, "shell", Head, d, t.row.speed, t.row.rate, t.Pierce, 1f) { hitR = 0.22f };
                        if (i == 0) Fx.Muzzle(Head, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, Projectile.ShotColor("shell"), 0.9f);
                        bool first = true;
                        pr.onHit = (p, e) =>
                        {
                            Combat.Hit(t, e, p.rate);
                            if (first && t.Has("split")) Split(p.pos, p.dir, (int)t.S("split"));
                            first = false;
                        };
                        b.Add(pr);
                    }
                    break;
                case "laser":
                {
                    float ang = Mathf.Atan2(target.Pos.y - Head.y, target.Pos.x - Head.x) * Mathf.Rad2Deg;
                    float w = t.row.radius * t.Area, sweep = t.Has("sweep") ? 22f : 0f;
                    b.Add(new Beam(t, Head, ang, t.Duration, t.row.tick, w, t.row.rate, sweep));
                    int extra = (int)t.S("beam_split");
                    for (int i = 0; i < extra; i++)
                        b.Add(new Beam(t, Head, ang + ((i % 2 == 0) ? 1 : -1) * 14f * (i / 2 + 1), t.Duration, t.row.tick, w * 0.7f, t.row.rate * 0.6, sweep));
                    break;
                }
                case "field":
                {
                    float half = t.row.radius * t.Area;
                    int n = 1 + (int)t.S("count_free");
                    if (Tables.Ci("Field_Vertical", 1) == 1)
                    {   // 8차 "끈끈이 드랍은 세로로": 적 무리 x 에 세로 띠 (여러 개면 오른쪽 · 왼쪽으로 번갈아, 끝에 닿으면 반대쪽)
                        float x0 = World.ShieldX + half + 0.2f, x1 = World.HalfW - half - 0.1f;
                        float x = Mathf.Clamp(b.CrowdX(), x0, x1);
                        for (int i = 0; i < n; i++)
                        {
                            float off = (i + 1) / 2 * 2.6f * (i % 2 == 1 ? 1 : -1);
                            float xi = x + off;
                            if (xi > x1 || xi < x0) xi = x - off;
                            b.Add(new Field(t, Mathf.Clamp(xi, x0, x1), half, t.Duration, t.Tick, t.row.rate));
                        }
                        break;
                    }
                    float y = Mathf.Clamp(b.CrowdY(), World.GroundY + half + 0.3f, 7.5f);
                    for (int i = 0; i < n; i++)
                        b.Add(new Field(t, Mathf.Clamp(y + i * 2.6f, World.GroundY + half + 0.3f, 8.5f), half, t.Duration, t.Tick, t.row.rate));
                    break;
                }
                case "hive":
                    for (int i = 0; i < t.Shots; i++) b.Add(new Bee(t, Head + new Vector2(Random.Range(-0.4f, 0.4f), 0.1f * i), t.row.hits + (int)t.S("hits"), t.row.rate));
                    break;
                case "missile":
                    for (int i = 0; i < t.Shots; i++)
                    {
                        var d = Rot(HeadDir, (i - (t.Shots - 1) / 2f) * 25f);   // 11-18: 머리(45°) 쪽으로 나감
                        var m = new Projectile(t, "missile", Head, d, t.row.speed, t.row.rate, 0, 1f) { target = target, turn = 4.5f, life = 5f, hitR = 0.25f };
                        Fx.Sparks(Head, Projectile.ShotColor("missile"), 4, 2.5f, 0.9f, 80f, -90f + Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
                        m.onHit = (p, e) => MissileHit(p.pos, e, 1.0);
                        b.Add(m);
                    }
                    break;
                case "cannon":
                    for (int i = 0; i < t.Shots; i++)
                    {
                        var d = Rot((target.Pos - Head).normalized, (i - (t.Shots - 1) / 2f) * 9f);
                        // 11-16 저녁 사용자 "발사체 크기 2배"
                        var pr = new Projectile(t, "cannonball", Head, d, t.row.speed, t.row.rate, t.Pierce, 2f) { hitR = 0.42f, life = 2.5f };
                        if (i == 0) Fx.Muzzle(Head, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, Projectile.ShotColor("cannonball"), 1.2f);
                        bool first = true;
                        pr.onHit = (p, e) => { CannonHit(p.pos, p.dir, e, first); first = false; };
                        b.Add(pr);
                    }
                    break;
                case "focus":
                {
                    float w = t.row.radius * t.Area;
                    b.Add(new FocusBeam(t, target, t.Duration, w));
                    int extra = (int)t.S("beams");
                    var skip = new System.Collections.Generic.HashSet<Enemy> { target };
                    for (int i = 0; i < extra; i++)
                    {
                        var e2 = b.Nearest(Head, t.Range, skip); if (e2 == null) break; skip.Add(e2);
                        b.Add(new FocusBeam(t, e2, t.Duration, w * 0.8f, target));
                    }
                    break;
                }
                case "vortex":
                {
                    float spd = t.row.speed, pull = 3.6f * (1 + (float)t.S("pull_pow") / 100f);   // 11-16 저녁 "끌어들이는 효과를 조금 더 강하게" 2.4 → 3.6
                    for (int i = 0; i < t.Shots; i++)
                    {
                        var d = Rot((target.Pos - Head).normalized, (i - (t.Shots - 1) / 2f) * 18f);
                        b.Add(new Vortex(t, Head + d * 0.3f, d, spd, t.Duration, t.row.radius * t.Area, t.row.rate, pull));
                    }
                    Fx.Glow(Head, 0.6f, new Color(0.8f, 0.7f, 1f, 0.7f), 0.2f);
                    break;
                }
                case "wheel":
                {
                    float spd = t.row.speed * (1 + (float)t.S("speed") / 100f);
                    int hits = Mathf.RoundToInt(t.row.hits * (1 + (float)t.S("hits_pct") / 100f) + (float)t.S("hits"));
                    for (int i = 0; i < t.Shots; i++)
                    {
                        var d = Rot((target.Pos - Head).normalized, (i - (t.Shots - 1) / 2f) * 30f + Random.Range(-12f, 12f));
                        b.Add(new FireWheel(t, Head, d * spd, t.row.radius * t.Area, hits, t.Duration, t.row.rate));
                    }
                    Fx.Sparks(Head, Art.Hex("#ffb35c"), 6, 3f, 1f, 120f, 90f);
                    break;
                }
                case "tesla":
                {
                    bool oc = t.Has("overcharge");
                    float charge = t.row.dur + (oc ? 0.5f : 0f);
                    float rad = t.row.radius * t.Area * (oc ? 1.4f : 1f);
                    int bounces = t.row.hits + (int)t.S("bounce") + (int)t.S("bounce_free");
                    int n = 1 + (int)t.S("shots_free");
                    for (int i = 0; i < n; i++)
                    {
                        var tg = i == 0 ? target : (b.RandomEnemy() ?? target);
                        b.Add(new TeslaBolt(t, tg, Head, charge + i * 0.12f, bounces, t.row.rate, rad));
                    }
                    break;
                }
                case "bolt":
                {
                    float w = t.row.radius * 2 * t.Area;
                    float x0 = target.Pos.x;
                    for (int i = 0; i < t.Shots; i++)
                    {
                        float x = i == 0 ? x0 : (b.RandomEnemy()?.Pos.x ?? x0 + (i % 2 == 0 ? 1 : -1) * 1.5f * i);
                        b.Add(new Strike(t, x, w, t.Duration, t.row.tick, t.row.rate));
                    }
                    break;
                }
            }
        }

        // ---------- 가디언 ----------
        void Gun(Enemy target, double mul, double rate, bool chargeShot = false)
        {
            var b = Battle.I;
            muzzle = 1 - muzzle;
            recoil = 0.07f; PoseGun();
            int shots = 1 + (int)t.S("shots") + (int)t.S("shots_free");
            if (chargeShot) shots = Mathf.CeilToInt(shots * (1 + (float)t.S("charge_shots") / 100f));
            var from = Head;
            var dir = (target.Pos - from).normalized;
            if (chargeShot) Audio.Play(Audio.Sfx.Gun, 0.8f);
            Fx.Muzzle(from, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, Projectile.ShotColor(chargeShot ? "bullet_charge" : "bullet"), chargeShot ? 1.3f : 0.9f);
            for (int i = 0; i < shots; i++)
            {
                var d = Rot(dir, (i - (shots - 1) / 2f) * 5f);
                var pr = new Projectile(t, chargeShot ? "bullet_charge" : "bullet", from, d, t.row.speed, rate * mul, t.Pierce + (chargeShot ? 3 : 0), 1f);
                pr.onHit = (p, e) =>
                {
                    Combat.Hit(t, e, p.rate);
                    if (t.Has("charge") && !Charging) charge += Tables.C("Charge_Per_Hit", 1) * (1 + t.S("charge_speed") / 100.0);
                };
                b.Add(pr);
            }
            shotsFired++;
            if (t.Has("charge") && charge >= Tables.C("Charge_Need", 120)) { charge = 0; chargeT = Tables.Cf("Charge_Time", 5f); }
        }

        void ChargeStep(float dt)
        {
            chargeT -= dt; chargeShotT -= dt;
            if (chargeShotT > 0) return;
            chargeShotT = Tables.Cf("Charge_Interval", 0.1f);
            var target = Battle.I.Nearest(Head, t.Range);
            if (target != null) Gun(target, Tables.C("Charge_Mul", 5), t.row.rate, true);
        }

        // ---------- 관통포 · 미사일 갈래 ----------
        void Split(Vector2 at, Vector2 dir, int n)
        {
            double rate = t.row.rate * 0.4 * (1 + t.S("split_dmg") / 100.0);
            for (int i = 0; i < n; i++)
            {
                var d = Rot(dir, (i - (n - 1) / 2f) * 28f);
                var pr = new Projectile(t, "shell", at + d * 0.3f, d, t.row.speed * 0.8f, rate, 0, 0.55f) { life = 1.2f };
                pr.onHit = (p, e) => Combat.Hit(t, e, p.rate, false);
                Battle.I.Add(pr);
            }
        }

        void MissileHit(Vector2 at, Enemy e, double mul)
        {
            Combat.Hit(t, e, t.row.rate * mul);
            float r = t.row.radius * t.Area * (float)(mul < 1 ? 0.6 : 1);
            Combat.Explode(t, at, r, t.row.rate2 * mul, Elem.Color("fire"));
            if (mul >= 1 && t.Has("split"))
            {
                int n = (int)t.S("split");
                for (int i = 0; i < n; i++)
                {
                    // 11-16: 맞은 자리에서 튀어나오던 것 → 미사일 터렛에서 발사해 맞은 자리 근처 적을 쫓음
                    var d = Rot(HeadDir, (i - (n - 1) / 2f) * 22f);
                    var m = new Projectile(t, "mini_missile", Head, d, t.row.speed * 0.9f, 0, 0, 1f) { turn = 6f, life = 4f, hitR = 0.18f, target = Battle.I.Nearest(at, 4f) };
                    double k = 0.4 * (1 + t.S("split_dmg") / 100.0);
                    m.onHit = (p, en) => MissileHit(p.pos, en, k);
                    Battle.I.Add(m);
                }
            }
            if (mul >= 1 && t.Has("fire_zone"))
                Battle.I.Add(new Zone(t, at, 1.0f * t.Area, 5f, 0.5f, 30, Elem.Color("fire")));
        }

        // ---------- 쾅쾅 대포 (2차) ----------
        /// <summary>포탄이 맞으면: 충격 피해 + 둥근 폭발(Rate2) + 폭발 안 적을 위로 밀어냄 · 첫 적중에 산탄 · 기뢰</summary>
        void CannonHit(Vector2 at, Vector2 dir, Enemy e, bool first)
        {
            Combat.Hit(t, e, t.row.rate);
            float r = t.row.radius * t.Area;
            Combat.Explode(t, at, r, t.row.rate2, Art.Hex("#ffcf6b"));
            float kn = Tables.Cf("Knock_Base", 0.6f) * t.row.knock * (1 + (float)t.S("knock") / 100f);
            foreach (var en in Battle.I.enemies) if (!en.dead && Vector2.Distance(en.Pos, at) <= r + en.radius) en.Knock(kn);
            if (!first) return;
            int sc = (int)t.S("scatter");
            if (sc > 0)
            {
                double sr = Tables.C("Scatter_Rate", 60) * (1 + t.S("scatter_dmg") / 100.0);
                for (int i = 0; i < sc; i++)
                {
                    var d = Rot(dir, (i - (sc - 1) / 2f) * (60f / Mathf.Max(1, sc - 1)));
                    var pr = new Projectile(t, "shell", at + d * 0.3f, d, t.row.speed * 1.2f, sr, 3, 0.5f) { life = 0.7f, hitR = 0.16f };
                    pr.onHit = (p, en) => Combat.Hit(t, en, p.rate, false);
                    Battle.I.Add(pr);
                }
            }
            int mines = (int)t.S("mines");
            for (int i = 0; i < mines; i++)
            {
                float a = (i / (float)mines) * Mathf.PI * 2 + Random.Range(0f, 0.6f);
                Battle.I.Add(new Mine(t, at, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 2.2f, Tables.C("Mine_Rate", 120) * (1 + t.S("mine_dmg") / 100.0), t.Has("mine_knock")));
            }
        }

        static Vector2 Rot(Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
    }
}
