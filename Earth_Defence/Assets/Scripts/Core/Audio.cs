using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace EarthDef
{
    /// <summary>
    /// 소리 (11-16 저녁, 사용자 "카치건과 비슷하게 · 잔잔함에 약간 몽환적인 느낌"): 음원 파일 없이 코드로 합성.
    /// 카치건 AudioManager 의 방식(합성 · 채널 나눔 · 같은 소리 간격 제한 · 배경음 겹쳐 바꾸기)을 가져오고 소리 결은 새로:
    ///   배경음 3곡 = 로비 "별빛 기지"(68 BPM · 드럼 없음) · 전투 "꿈속 순찰"(84 BPM · 아주 약한 킥/쉐이커) · 보스 "검은 행성"(96 BPM · 단조 · 낮은 맥박)
    ///   악기 = 겹친 패드(살짝 어긋난 사인 2개 · 느린 일렁임) · 첼레스타 종소리 멜로디 · 유리 같은 분산화음 · 부드러운 베이스
    ///   몽환 = 멜로디에 메아리(점4분 · 되먹임) + 전체에 잔향(Schroeder) — 반복 이음새가 안 끊기게 두 바퀴 돌려 둘째 바퀴를 씀
    ///   효과음 = 사인 · 삼각 중심의 둥근 소리 + 일부 짧은 잔향 (톱니 · 거친 잡음은 줄임)
    /// 음량: Config Bgm_Volume · Sfx_Volume x 설정(SaveData.bgm · sfx). 배경음은 로딩이 길어 일꾼 스레드에서 미리 계산.
    /// 숫자를 바꾸면 tools/sound_review.py 로 다시 재 볼 것 (PlayTest.RunSound 가 wav 를 뽑음).
    /// </summary>
    public class Audio : MonoBehaviour
    {
        public static Audio I;
        public enum Sfx
        {
            UiClick, Gun, Rail, Laser, Field, Bee, Missile, Boom, BigBoom, Bolt, Focus, Vortex, Wheel, Tesla, Zap,
            Hit, Kill, BaseHit, LevelUp, CardPick, Break, Place, BossWarn, EliteWarn, Victory, Defeat, Coin, Upgrade, Merge,
        }
        public const int SR = 44100;

        // ---------- 시험 · 검토용 ----------
        /// <summary>null 이 아니면 재생한 효과음을 (시각, 소리, 음량, 높낮이) 로 적음 — 전투 장면 섞기(tools/sound_review.py)</summary>
        public static List<(float t, Sfx s, float vol, float pitch)> Record;
        public static readonly Dictionary<Sfx, float[]> SfxData = new();
        public static readonly Dictionary<string, float[]> BgmData = new();
        public static int Played;

        readonly Dictionary<Sfx, AudioClip> clips = new();
        readonly Dictionary<Sfx, float> last = new();
        AudioSource[] sfxSrc;
        readonly AudioSource[] bgmSrc = new AudioSource[2];
        int bgmCur, chan;
        float fadeT = 1f, duck = 1f, duckWant = 1f;
        string want = "lobby", playing = "";
        readonly Dictionary<string, AudioClip> bgmClips = new();
        readonly Dictionary<string, Task<float[]>> tasks = new();
        Task<Dictionary<Sfx, float[]>> sfxTask;
        /// <summary>효과음이 다 만들어졌나 (시험 · 소리 뽑기가 기다림)</summary>
        public static bool SfxReady => I && I.sfxTask == null;
        /// <summary>검토용 원본 배열(SfxData · BgmData)은 에디터에서만 남김 — 폰에서는 소리 조각으로 바꾼 뒤 버려 메모리를 아낌 (배경음 3곡 = 같은 크기가 두 벌이었음)</summary>
        static bool KeepData => Application.isEditor;
        const int Channels = 16, CombatChannels = 10;

        static SaveData S => SaveData.Cur;
        float BgmVol => Mathf.Clamp01(Tables.Cf("Bgm_Volume", 0.7f)) * (S != null ? S.bgm : 0.8f);
        float SfxVol => Mathf.Clamp01(Tables.Cf("Sfx_Volume", 0.75f)) * (S != null ? S.sfx : 0.8f);

        public static void Ensure()
        {
            if (I) return;
            var go = new GameObject("Audio");
            DontDestroyOnLoad(go);
            I = go.AddComponent<Audio>();
            I.Init();
        }

        void Init()
        {
            if (FindFirstObjectByType<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
            for (int i = 0; i < 2; i++)
            {
                var g = new GameObject("Bgm" + i); g.transform.SetParent(transform, false);
                var a = g.AddComponent<AudioSource>(); a.loop = true; a.playOnAwake = false; a.volume = 0;
                bgmSrc[i] = a;
            }
            var sg = new GameObject("Sfx"); sg.transform.SetParent(transform, false);
            sfxSrc = new AudioSource[Channels];
            for (int i = 0; i < Channels; i++) { sfxSrc[i] = sg.AddComponent<AudioSource>(); sfxSrc[i].playOnAwake = false; }
            // 11-17 최적화: 효과음 29개 합성(PC 0.38초 · 폰은 몇 초)을 켤 때 화면을 멈추고 하던 것 → 일꾼 스레드에서 만들고, 다 되면 Update 가 소리 조각으로 바꿈
            try { sfxTask = Task.Run(SynthSfx); } catch { sfxTask = Task.FromResult(SynthSfx()); }
            // 배경음은 길어서 일꾼 스레드에서 (로비 먼저)
            foreach (var n in new[] { "lobby", "battle", "boss" })
            {
                string name = n;
                try { tasks[name] = Task.Run(() => Song(name)); } catch { BgmData[name] = Song(name); }
            }
        }

        // ================================================================ 재생
        public static void Play(Sfx s, float volMul = 1f) { if (I) I.PlaySfx(s, volMul); }
        /// <summary>배경음 바꾸기: lobby · battle · boss · "" (끔)</summary>
        public static void Music(string name) { if (I) { I.want = name; I.duckWant = 1f; } }
        /// <summary>결과 창 등에서 배경음을 잠깐 작게</summary>
        public static void Duck(float k) { if (I) I.duckWant = k; }
        public static string Playing => I ? I.playing : "";

        float Now => Battle.I != null && Battle.I.st != Battle.St.End ? Battle.I.time : Time.unscaledTime;

        static float MinGap(Sfx s) => s switch
        {
            Sfx.Gun => 0.11f, Sfx.Hit => 0.07f, Sfx.Kill => 0.05f, Sfx.Boom => 0.09f, Sfx.Zap => 0.06f, Sfx.Bee => 0.15f,
            Sfx.BaseHit => 0.15f, Sfx.Rail or Sfx.Missile or Sfx.Laser or Sfx.Bolt or Sfx.Field or Sfx.Focus or Sfx.Vortex or Sfx.Wheel or Sfx.Tesla or Sfx.BigBoom => 0.08f,
            _ => 0.03f,
        };
        static bool Combat(Sfx s) => s <= Sfx.BaseHit && s != Sfx.UiClick;

        void PlaySfx(Sfx s, float volMul)
        {
            float now = Now;
            if (last.TryGetValue(s, out var l) && now - l < MinGap(s) && now >= l) return;
            last[s] = now;
            float vol = SfxVol * volMul;
            float pitch = s is Sfx.Gun or Sfx.Hit or Sfx.Kill or Sfx.Zap or Sfx.Bee ? UnityEngine.Random.Range(0.94f, 1.06f) : 1f;
            Played++;
            Record?.Add((now, s, vol, pitch));
            if (vol <= 0.001f || !clips.TryGetValue(s, out var c)) return;
            bool cb = Combat(s);
            int idx = Free(cb ? 0 : CombatChannels, cb ? CombatChannels : Channels);
            if (idx < 0 && !cb) idx = Free(0, Channels);
            if (idx < 0) return;
            chan = idx;
            var a = sfxSrc[idx]; a.clip = c; a.volume = vol; a.pitch = pitch; a.Play();
        }

        int Free(int from, int to)
        {
            int n = to - from;
            for (int i = 0; i < n; i++) { int k = from + (Mathf.Clamp(chan, from, to - 1) - from + i) % n; if (!sfxSrc[k].isPlaying) return k; }
            return -1;
        }

        AudioClip BgmClip(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (bgmClips.TryGetValue(name, out var c)) return c;
            if (!BgmData.TryGetValue(name, out var d))
            {
                if (!tasks.TryGetValue(name, out var t) || !t.IsCompleted) return null;    // 아직 계산 중 → 다음 프레임에
                d = t.Result; tasks.Remove(name);
                if (KeepData) BgmData[name] = d;
            }
            c = AudioClip.Create("Bgm_" + name, d.Length, 1, SR, false); c.SetData(d, 0);
            return bgmClips[name] = c;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (sfxTask != null && sfxTask.IsCompleted)
            {
                foreach (var kv in sfxTask.Result)
                {
                    if (KeepData) SfxData[kv.Key] = kv.Value;
                    var c = AudioClip.Create("Sfx_" + kv.Key, kv.Value.Length, 1, SR, false); c.SetData(kv.Value, 0); clips[kv.Key] = c;
                }
                sfxTask = null;
            }
            // 전투 · 보스 곡의 소리 조각을 로비에서 쉬는 동안 미리 만들어 둠 (예전엔 첫 전투 · 첫 보스 순간에 만들며 멈칫) — 한 프레임에 한 곡
            else if (sfxTask == null && want == playing && fadeT >= 1f && tasks.Count > 0)
            {
                string ready = null;
                foreach (var kv in tasks) if (kv.Value.IsCompleted) { ready = kv.Key; break; }
                if (ready != null) BgmClip(ready);
            }
            duck = Mathf.MoveTowards(duck, duckWant, dt * 1.5f);
            if (want != playing)
            {
                var clip = BgmClip(want);
                if (clip != null || string.IsNullOrEmpty(want))
                {
                    bgmCur = 1 - bgmCur;
                    var nx = bgmSrc[bgmCur];
                    nx.clip = clip; nx.volume = 0;
                    if (clip) nx.Play();
                    fadeT = 0; playing = want;
                }
            }
            if (fadeT < 1f) fadeT = Mathf.Min(1f, fadeT + dt / 1.2f);       // 1.2초 겹쳐 바꾸기 (잔잔하게)
            float v = BgmVol * duck;
            bgmSrc[bgmCur].volume = v * fadeT;
            var prev = bgmSrc[1 - bgmCur];
            prev.volume = v * (1 - fadeT);
            if (fadeT >= 1f && prev.isPlaying) { prev.Stop(); prev.clip = null; }
        }

        // ================================================================ 효과음 만들기
        /// <summary>효과음 원본 배열 만들기 (일꾼 스레드에서 돎 — 엔진 함수를 부르지 않음)</summary>
        static Dictionary<Sfx, float[]> SynthSfx()
        {
            var made = new Dictionary<Sfx, float[]>();
            void Add(Sfx s, float[] d) { Edge(d); made[s] = d; }
            // 대장 카피 소총: 아주 짧고 둥근 "퓻" (1초에 7번 → 작게)
            Add(Sfx.Gun, Mix(Sweep(1500, 820, 0.055f, W.Sine, 0.20f, 7f), Sweep(3000, 1640, 0.04f, W.Sine, 0.05f, 9f)));
            // 관통포: 유리 같은 "휘익 - 띵"
            Add(Sfx.Rail, Space(Mix(Sweep(900, 320, 0.14f, W.Tri, 0.32f, 4f), Bell(1760, 0.35f, 0.13f)), 0.25f, 0.6f));
            // 레이저 포: 반짝이며 떨리는 두 음
            Add(Sfx.Laser, Space(Mix(Wobble(880, 0.42f, 0.16f, 22f, 0.012f), Wobble(1318.5f, 0.42f, 0.10f, 17f, 0.01f)), 0.3f, 0.7f));
            // 끈끈이 드론: 낮게 웅 — 장판이 깔림
            // (11-16 측정: 220Hz 는 폰 스피커에서 안 들려 한 옥타브 위로)
            Add(Sfx.Field, Space(Mix(Wobble(440, 0.7f, 0.15f, 5f, 0.004f), Wobble(660, 0.7f, 0.09f, 6f, 0.004f), Wobble(220, 0.7f, 0.08f, 5f, 0.004f)), 0.3f, 0.7f));
            // 벌집: 부드러운 "부르르"
            Add(Sfx.Bee, Wobble(640, 0.14f, 0.12f, 38f, 0.03f, W.Tri));
            // 유도 미사일: 쉬익 (걸러진 잡음 + 올라가는 음)
            Add(Sfx.Missile, Mix(Noise(0.32f, 2.5f, 0.10f, 0.5f, 11), Sweep(300, 560, 0.3f, W.Sine, 0.12f, 3f), Sweep(600, 1120, 0.3f, W.Sine, 0.06f, 3f)));
            // 폭발: 둥근 "쿵" + 낮은 잡음 + 잔향
            // 폭발: 낮은 "쿵"만으로는 폰에서 안 들려(측정 0%) 가운데 소리(밝은 잡음 · 내려가는 삼각파)를 섞음
            Add(Sfx.Boom, Space(Mix(Thump(0.42f, 120, 42, 0.36f), Noise(0.3f, 8f, 0.2f, 0.35f, 21), Sweep(620, 180, 0.2f, W.Tri, 0.12f, 5f)), 0.25f, 0.6f));
            // 쾅쾅 대포 (쏨): 더 낮고 큰 "쿵"
            Add(Sfx.BigBoom, Space(Mix(Thump(0.55f, 95, 34, 0.45f), Noise(0.36f, 7f, 0.24f, 0.3f, 23), Sweep(520, 150, 0.26f, W.Tri, 0.14f, 4f)), 0.3f, 0.65f));
            // 벼락: 지직거리는 잡음 덩어리 + 내려가는 높은 음
            Add(Sfx.Bolt, Space(Mix(Crackle(0.3f, 0.22f, 31), Sweep(2200, 700, 0.18f, W.Sine, 0.12f, 5f)), 0.25f, 0.6f));
            // 집중 빔: 지글지글 (높은 잡음이 빠르게 깜빡 + 떨리는 높은 음)
            Add(Sfx.Focus, Mix(Crackle(0.45f, 0.11f, 41, 0.9f), Wobble(1200, 0.45f, 0.08f, 30f, 0.015f)));
            // 소용돌이포: 빨려드는 "후우웅" (내려가는 음 + 일렁임)
            Add(Sfx.Vortex, Space(Mix(Wobble(480, 0.55f, 0.14f, 7f, 0.05f, W.Sine, 0.35f), Noise(0.5f, 3f, 0.08f, 0.15f, 51)), 0.35f, 0.7f));
            // 불바퀴: 불길 "화르르"
            Add(Sfx.Wheel, Mix(Noise(0.45f, 3f, 0.2f, 0.25f, 61, true), Crackle(0.4f, 0.1f, 63)));
            // 찌릿 코일: 충전되며 올라가는 음 → 찌릿
            Add(Sfx.Tesla, Mix(Sweep(300, 1500, 0.32f, W.Tri, 0.16f, 0.3f), Delay(Crackle(0.12f, 0.2f, 71), 0.3f)));
            Add(Sfx.Zap, Crackle(0.09f, 0.14f, 73));
            // 맞음: 아주 작은 "틱"
            Add(Sfx.Hit, Mix(Sweep(2300, 1800, 0.03f, W.Sine, 0.08f, 9f), Noise(0.02f, 40f, 0.05f, 1f, 81)));
            // 적 처치: 비눗방울 "뽁" + 작은 반짝
            Add(Sfx.Kill, Mix(Sweep(420, 1050, 0.07f, W.Sine, 0.2f, 4f), Delay(Bell(2637, 0.12f, 0.05f), 0.04f)));
            // 기지가 맞음: 먹먹한 "퉁"
            Add(Sfx.BaseHit, Mix(Thump(0.22f, 85, 50, 0.3f), Sweep(440, 260, 0.13f, W.Tri, 0.17f, 6f), Noise(0.12f, 12f, 0.12f, 0.4f, 91)));
            // 레벨 업: 올라가는 종소리 (D 장조) + 잔향
            Add(Sfx.LevelUp, Space(Arp(new[] { 74, 78, 81, 86 }, 0.075f, 0.2f, 1.6f), 0.4f, 0.75f));
            Add(Sfx.CardPick, Space(Arp(new[] { 81, 86 }, 0.07f, 0.2f, 0.9f), 0.3f, 0.6f));
            // 돌파: 반짝반짝 쓸어 올림 (5음 음계 8개)
            Add(Sfx.Break, Space(Arp(new[] { 74, 76, 78, 81, 83, 86, 88, 90 }, 0.045f, 0.16f, 1.2f), 0.45f, 0.8f));
            // 새 터렛 놓임: "톡" + 종
            Add(Sfx.Place, Space(Mix(Thump(0.18f, 220, 110, 0.3f), Delay(Bell(1174.7f, 0.6f, 0.16f), 0.05f)), 0.3f, 0.65f));
            // 보스 등장: 낮은 두 음이 부풀었다 사라짐 (어둡지만 부드럽게)
            // (측정: 낮은 화음이라 폰에서 안 들려 한 옥타브 위가 주인공 · 낮은 음은 살짝)
            Add(Sfx.BossWarn, Space(Mix(Swell(new[] { 59, 62, 66 }, 1.6f, 0.19f, 2f), Swell(new[] { 71, 78 }, 1.6f, 0.1f, 2f), Swell(new[] { 47 }, 1.6f, 0.06f, 2f)), 0.45f, 0.8f));
            Add(Sfx.EliteWarn, Space(Mix(Swell(new[] { 66, 69, 73 }, 1.0f, 0.11f, 3f), Swell(new[] { 54 }, 1.0f, 0.05f, 3f)), 0.4f, 0.75f));
            // 승리: 종소리 분산화음 + 화음이 부풀어 오름
            Add(Sfx.Victory, Space(Mix(Arp(new[] { 74, 78, 81, 86, 90, 93 }, 0.11f, 0.2f, 2.4f), Delay(Swell(new[] { 62, 66, 69, 74 }, 2.2f, 0.06f, 0f), 0.25f)), 0.5f, 0.85f));
            // 패배: 천천히 내려가는 종 (B 단조)
            Add(Sfx.Defeat, Space(Mix(Arp(new[] { 78, 74, 71, 66 }, 0.24f, 0.17f, 2.4f), Swell(new[] { 59, 66, 71 }, 2.2f, 0.045f, 0f)), 0.5f, 0.85f));
            Add(Sfx.Coin, Arp(new[] { 95, 100 }, 0.05f, 0.12f, 1.5f));
            Add(Sfx.Upgrade, Space(Arp(new[] { 74, 81, 86 }, 0.05f, 0.16f, 1.3f), 0.3f, 0.6f));
            Add(Sfx.Merge, Space(Arp(new[] { 78, 81, 86, 90 }, 0.06f, 0.16f, 1.5f), 0.35f, 0.7f));
            Add(Sfx.UiClick, Mix(Sweep(900, 760, 0.045f, W.Sine, 0.2f, 6f), Sweep(1800, 1520, 0.03f, W.Sine, 0.05f, 8f)));
            return made;
        }

        enum W { Sine, Tri }
        static float Wave(double ph, W w) { double f = ph - Math.Floor(ph); return w == W.Tri ? (float)(f < 0.5 ? 4 * f - 1 : 3 - 4 * f) : (float)Math.Sin(2 * Math.PI * ph); }
        public static float Hz(int m) => 440f * Mathf.Pow(2f, (m - 69) / 12f);

        /// <summary>앞뒤 3ms 부드럽게 (딸깍 방지)</summary>
        static void Edge(float[] d) { int n = Mathf.Min(d.Length / 2, (int)(SR * 0.003f)); for (int i = 0; i < n; i++) { float k = (float)i / n; d[i] *= k; d[d.Length - 1 - i] *= k; } }

        static float[] Mix(params float[][] a)
        {
            int n = 0; foreach (var x in a) n = Math.Max(n, x.Length);
            var r = new float[n];
            foreach (var x in a) for (int i = 0; i < x.Length; i++) r[i] += x[i];
            for (int i = 0; i < n; i++) r[i] = Mathf.Clamp(r[i], -1f, 1f);
            return r;
        }
        static float[] Delay(float[] a, float sec) { int o = (int)(SR * sec); var r = new float[a.Length + o]; Array.Copy(a, 0, r, o, a.Length); return r; }

        /// <summary>높낮이가 미끄러지는 음. decay = 끝까지 몇 배로 줄어드나(지수)</summary>
        static float[] Sweep(float f0, float f1, float sec, W w, float gain, float decay)
        {
            int n = (int)(SR * sec); var d = new float[n]; double ph = 0;
            for (int i = 0; i < n; i++) { float t = (float)i / n; ph += Mathf.Lerp(f0, f1, t) / SR; d[i] = Wave(ph, w) * Mathf.Exp(-decay * t) * gain; }
            return d;
        }
        /// <summary>떨림 있는 음 (vib Hz · 깊이) · 부드럽게 들어오고 나감 · drop = 끝까지 높낮이가 이만큼 내려감(비율)</summary>
        static float[] Wobble(float f, float sec, float gain, float vib, float depth, W w = W.Sine, float drop = 0f)
        {
            int n = (int)(SR * sec); var d = new float[n]; double ph = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR, k = (float)i / n;
                ph += f * (1 - drop * k) * (1 + depth * Mathf.Sin(2 * Mathf.PI * vib * t)) / SR;
                float env = Mathf.Min(1f, t / 0.03f) * Mathf.Pow(1 - k, 1.5f);
                d[i] = Wave(ph, w) * env * gain;
            }
            return d;
        }
        /// <summary>첼레스타 같은 종 (사인 + 2배 · 4배 음이 빨리 사라짐)</summary>
        static float[] Bell(float f, float sec, float gain)
        {
            int n = (int)(SR * sec); var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR, w = 2 * Mathf.PI * f * t;
                float v = Mathf.Sin(w) + 0.3f * Mathf.Sin(2 * w) * Mathf.Exp(-5 * t) + 0.12f * Mathf.Sin(4.01f * w) * Mathf.Exp(-12 * t);
                d[i] = v * Mathf.Min(1, t / 0.003f) * Mathf.Exp(-3.2f * t / Mathf.Max(0.3f, sec * 0.6f)) * gain;
            }
            return d;
        }
        static float[] Arp(int[] notes, float step, float gain, float lastSec)
        {
            var parts = new List<float[]>();
            for (int i = 0; i < notes.Length; i++)
                parts.Add(Delay(Bell(Hz(notes[i]), i == notes.Length - 1 ? lastSec : Mathf.Max(0.35f, step * 4), gain * (0.8f + 0.2f * i / notes.Length)), step * i));
            return Mix(parts.ToArray());
        }
        /// <summary>화음이 부풀었다 사라짐 (어긋난 사인 2개 · pulse = 일렁임 Hz)</summary>
        static float[] Swell(int[] chord, float sec, float gain, float pulse)
        {
            int n = (int)(SR * sec); var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR, k = (float)i / n, v = 0;
                foreach (int m in chord) { float f = Hz(m); v += Mathf.Sin(2 * Mathf.PI * f * t) + 0.7f * Mathf.Sin(2 * Mathf.PI * f * 1.004f * t) + 0.15f * Mathf.Sin(6 * Mathf.PI * f * t); }
                float env = Mathf.Sin(Mathf.PI * Mathf.Pow(k, 0.6f)) * (pulse > 0 ? 0.75f + 0.25f * Mathf.Sin(2 * Mathf.PI * pulse * t) : 1f);
                d[i] = v * env * gain;
            }
            return d;
        }
        static float[] Thump(float sec, float f0, float f1, float gain)
        {
            int n = (int)(SR * sec); var d = new float[n]; double ph = 0;
            for (int i = 0; i < n; i++) { float k = (float)i / n; ph += Mathf.Lerp(f0, f1, Mathf.Sqrt(k)) / SR; d[i] = Mathf.Sin((float)(ph * 2 * Math.PI)) * Mathf.Exp(-6 * k) * gain; }
            return d;
        }
        /// <summary>잡음 (lp = 0~1 낮을수록 먹먹 · swell = 커졌다 작아짐)</summary>
        static float[] Noise(float sec, float decay, float gain, float lp, int seed, bool swell = false)
        {
            int n = (int)(SR * sec); var d = new float[n]; var r = new System.Random(seed); float y = 0;
            for (int i = 0; i < n; i++)
            {
                float k = (float)i / n, x = (float)(r.NextDouble() * 2 - 1);
                y += (x - y) * lp;
                float env = swell ? Mathf.Sin(Mathf.PI * k) : Mathf.Exp(-decay * k);
                d[i] = y * env * gain * (lp < 0.5f ? 1.8f : 1f);
            }
            return d;
        }
        /// <summary>지직 (짧은 잡음 조각이 띄엄띄엄 · rate = 조각이 나올 확률)</summary>
        static float[] Crackle(float sec, float gain, int seed, float rate = 0.6f)
        {
            int n = (int)(SR * sec); var d = new float[n]; var r = new System.Random(seed);
            int i = 0;
            while (i < n)
            {
                int len = (int)(SR * (0.002 + r.NextDouble() * 0.008));
                bool on = r.NextDouble() < rate;
                float a = (float)(0.4 + r.NextDouble() * 0.6) * (1 - (float)i / n);
                float prev = 0;
                for (int j = 0; j < len && i < n; j++, i++)
                {
                    float x = (float)(r.NextDouble() * 2 - 1);
                    d[i] = on ? (x - prev) * 0.6f * a * gain : 0; prev = x;
                }
                i += (int)(SR * r.NextDouble() * 0.006);
            }
            return d;
        }
        /// <summary>짧은 잔향 (효과음용): mix = 잔향 비율 · size 0~1</summary>
        static float[] Space(float[] x, float mix, float size)
        {
            int tail = (int)(SR * (0.25f + size * 0.9f));
            var a = new float[x.Length + tail]; Array.Copy(x, a, x.Length);
            var wet = Reverb(a, size, false);
            for (int i = 0; i < a.Length; i++) a[i] = Mathf.Clamp(a[i] * (1 - mix * 0.4f) + wet[i] * mix, -1, 1);
            int end = a.Length; while (end > x.Length && Mathf.Abs(a[end - 1]) < 0.0005f) end--;
            Array.Resize(ref a, Mathf.Max(x.Length, end));
            return a;
        }

        /// <summary>Schroeder 잔향 (빗살 4 + 통과 2, 빗살에 먹먹함) · loop = 반복 곡이면 두 바퀴 돌려 둘째 바퀴 (이음새 없음)</summary>
        static float[] Reverb(float[] x, float size, bool loop)
        {
            int[] comb = { 1557, 1617, 1491, 1422 }; int[] ap = { 556, 225 };
            float fb = 0.72f + 0.16f * size, damp = 0.35f;
            int n = x.Length, passes = loop ? 2 : 1;
            var outp = new float[n];
            var cb = new float[comb.Length][]; var ci = new int[comb.Length]; var cl = new float[comb.Length];
            for (int c = 0; c < comb.Length; c++) cb[c] = new float[(int)(comb[c] * (0.8f + 0.6f * size))];
            var ab = new float[ap.Length][]; var ai = new int[ap.Length];
            for (int c = 0; c < ap.Length; c++) ab[c] = new float[ap[c]];
            for (int p = 0; p < passes; p++)
                for (int i = 0; i < n; i++)
                {
                    float inp = x[i] * 0.2f, s = 0;
                    for (int c = 0; c < comb.Length; c++)
                    {
                        float y = cb[c][ci[c]];
                        cl[c] = y * (1 - damp) + cl[c] * damp;
                        cb[c][ci[c]] = inp + cl[c] * fb;
                        ci[c] = (ci[c] + 1) % cb[c].Length;
                        s += y;
                    }
                    for (int c = 0; c < ap.Length; c++)
                    {
                        float b = ab[c][ai[c]];
                        float y = -s * 0.6f + b;
                        ab[c][ai[c]] = s + b * 0.6f;
                        ai[c] = (ai[c] + 1) % ab[c].Length;
                        s = y;
                    }
                    if (p == passes - 1) outp[i] = s;
                }
            return outp;
        }

        /// <summary>메아리 (delay 초 · 되먹임 · 먹먹하게) — 반복 곡이면 두 바퀴</summary>
        static float[] Echo(float[] x, float sec, float fb, bool loop)
        {
            int n = x.Length, dl = (int)(SR * sec); var buf = new float[dl]; int bi = 0; float lp = 0;
            var outp = new float[n];
            for (int p = 0; p < (loop ? 2 : 1); p++)
                for (int i = 0; i < n; i++)
                {
                    float y = buf[bi];
                    lp += (y - lp) * 0.45f;
                    buf[bi] = x[i] + lp * fb;
                    bi = (bi + 1) % dl;
                    if (p == (loop ? 1 : 0)) outp[i] = lp;
                }
            return outp;
        }

        // ================================================================ 배경음
        // 8분음표 칸 · 한 마디 8칸. 화음은 마디마다 MIDI 번호 (낮은 것부터). 멜로디 0 = 쉼표
        class Sec { public int[][] chords; public int[] mel; public string drums = "........"; public bool arp, bass = true, twinkle; }
        static void AddWrap(float[] data, float[] src, int at, float gain = 1f)
        {
            int n = data.Length;
            for (int i = 0; i < src.Length; i++) data[(at + i) % n] += src[i] * gain;
        }

        static float[] PadNote(float f, float sec, float gain, float lfo)
        {
            int n = (int)(SR * (sec + 1.2f)); var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                float env = Mathf.Min(1, t / 0.9f) * (t > sec ? Mathf.Exp(-(t - sec) * 3.5f) : 1f);
                float v = Mathf.Sin(2 * Mathf.PI * f * t) + 0.8f * Mathf.Sin(2 * Mathf.PI * f * 1.0035f * t + 1.3f) + 0.12f * Mathf.Sin(6 * Mathf.PI * f * t);
                d[i] = v * env * gain * (0.85f + 0.15f * Mathf.Sin(2 * Mathf.PI * lfo * t + f));
            }
            return d;
        }
        static float[] Glass(float f, float sec, float gain)
        {
            int n = (int)(SR * sec); var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR, w = 2 * Mathf.PI * f * t;
                d[i] = (Mathf.Sin(w) + 0.2f * Mathf.Sin(3 * w) * Mathf.Exp(-20 * t)) * Mathf.Min(1, t / 0.004f) * Mathf.Exp(-5.5f * t) * gain;
            }
            return d;
        }
        static float[] BassNote(float f, float sec, float gain)
        {
            int n = (int)(SR * sec); var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                d[i] = (Mathf.Sin(2 * Mathf.PI * f * t) + 0.25f * Mathf.Sin(4 * Mathf.PI * f * t)) * Mathf.Min(1, t / 0.02f) * Mathf.Min(1, (sec - t) / 0.08f) * Mathf.Exp(-1.2f * t / sec) * gain;
            }
            return d;
        }

        /// <summary>곡 하나 계산 (일꾼 스레드 — 표 · 유니티 물건을 안 씀)</summary>
        public static float[] Song(string name)
        {
            int[] D = { 50, 54, 57, 61 }, Bm = { 47, 50, 54, 57 }, G = { 43, 47, 50, 54 }, Asus = { 45, 50, 52, 57 }, Em = { 40, 43, 47, 50 }, Fsm = { 42, 45, 49, 52 },
                  A = { 45, 49, 52, 57 }, Fs = { 42, 46, 49, 54 }, Em9 = { 40, 47, 50, 54 };
            float bpm; Sec[] secs; float target;
            switch (name)
            {
                case "battle":
                {
                    bpm = 84; target = 0.13f;
                    int[] m1 = { 78, 0, 76, 0, 74, 0, 0, 71, 0, 0, 74, 0, 76, 0, 0, 0, 78, 0, 81, 0, 79, 0, 78, 0, 76, 0, 0, 0, 74, 0, 0, 0 };
                    int[] m2 = { 81, 0, 0, 83, 0, 0, 81, 0, 78, 0, 0, 0, 76, 0, 78, 0, 74, 0, 0, 76, 0, 0, 78, 0, 81, 0, 0, 0, 0, 0, 0, 0 };
                    int[] m0 = new int[32];
                    secs = new[]
                    {
                        new Sec { chords = new[] { Bm, G, D, A }, mel = m0, arp = true, bass = false, twinkle = true },
                        new Sec { chords = new[] { Bm, G, D, A }, mel = m0, arp = true, drums = "k.t.k.t." },
                        new Sec { chords = new[] { Bm, G, D, A }, mel = m1, arp = true, drums = "k.tsk.ts" },
                        new Sec { chords = new[] { Em9, Fsm, G, A }, mel = m2, arp = true, drums = "k.tsk.ts" },
                        new Sec { chords = new[] { Bm, G, D, A }, mel = m1, arp = true, drums = "k.t.k.t.", twinkle = true },
                    };
                    break;
                }
                case "boss":
                {
                    bpm = 96; target = 0.15f;
                    int[] m1 = { 71, 0, 0, 74, 0, 73, 0, 71, 69, 0, 0, 0, 66, 0, 0, 0, 67, 0, 0, 71, 0, 69, 0, 67, 66, 0, 0, 0, 0, 0, 0, 0 };
                    int[] m2 = { 78, 0, 0, 0, 76, 0, 74, 0, 73, 0, 0, 0, 71, 0, 0, 0, 74, 0, 0, 0, 73, 0, 71, 0, 70, 0, 0, 0, 0, 0, 0, 0 };
                    secs = new[]
                    {
                        new Sec { chords = new[] { Bm, G, Em, Fs }, mel = new int[32], drums = "k...b...", arp = true, twinkle = true },
                        new Sec { chords = new[] { Bm, G, Em, Fs }, mel = m1, drums = "k.b.k.bt", arp = true },
                        new Sec { chords = new[] { Bm, G, Em, Fs }, mel = m2, drums = "k.btk.bt", arp = true },
                        new Sec { chords = new[] { G, Em, Bm, Fs }, mel = m1, drums = "k.b.k.bt", arp = true, twinkle = true },
                    };
                    break;
                }
                default:   // lobby
                {
                    bpm = 68; target = 0.11f;
                    int[] m1 = { 78, 0, 0, 81, 0, 0, 0, 0, 83, 0, 81, 0, 78, 0, 0, 0, 76, 0, 0, 78, 0, 0, 74, 0, 76, 0, 0, 0, 0, 0, 0, 0 };
                    int[] m2 = { 81, 0, 83, 0, 86, 0, 0, 0, 83, 0, 81, 0, 78, 0, 0, 0, 79, 0, 78, 0, 76, 0, 0, 0, 74, 0, 76, 0, 78, 0, 0, 0 };
                    int[] sp = { 0, 0, 0, 0, 0, 0, 0, 0, 81, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 78, 0, 0, 0, 0, 0, 0, 0 };
                    secs = new[]
                    {
                        new Sec { chords = new[] { D, Bm, G, Asus }, mel = sp, bass = false, twinkle = true },
                        new Sec { chords = new[] { D, Bm, G, Asus }, mel = m1 },
                        new Sec { chords = new[] { G, Fsm, Em, Asus }, mel = m2, arp = true },
                        new Sec { chords = new[] { D, Bm, G, Asus }, mel = m1, arp = true, twinkle = true },
                    };
                    break;
                }
            }
            int step = (int)(SR * 60f / bpm / 2f), steps = 0;
            foreach (var s in secs) steps += s.mel.Length;
            int n = step * steps;
            var dry = new float[n]; var bells = new float[n]; var pads = new float[n];
            var rnd = new System.Random(name == "lobby" ? 11 : name == "battle" ? 22 : 33);
            var kick = Thump(0.28f, 95, 45, 0.14f); var tom = Mix(Thump(0.3f, 150, 85, 0.07f), Sweep(420, 300, 0.12f, W.Tri, 0.03f, 6f));
            var shaker = Noise(0.06f, 9f, 0.025f, 0.9f, 5); var tick = Mix(Sweep(3200, 2900, 0.03f, W.Sine, 0.02f, 8f), Noise(0.02f, 30f, 0.015f, 1f, 7));
            var bossBass = name == "boss";
            int at = 0;
            foreach (var s in secs)
            {
                for (int b = 0; b < s.chords.Length; b++)
                {
                    var ch = s.chords[b]; int o = at + b * 8 * step; float barSec = 8f * step / SR;
                    foreach (int m in ch) AddWrap(pads, PadNote(Hz(m + 12), barSec, 0.05f, 0.25f), o);
                    if (s.bass)
                    {
                        if (bossBass) for (int k = 0; k < 8; k++) AddWrap(dry, BassNote(Hz(ch[0] - 12), step * 0.9f / SR, k % 4 == 0 ? 0.13f : 0.08f), o + k * step);   // (측정: 낮은 소리가 많아 폰 비율 24% → 줄임)
                        else { AddWrap(dry, BassNote(Hz(ch[0] - 12), barSec * 0.5f, 0.2f), o); AddWrap(dry, BassNote(Hz(ch[0] - 12 + 7), barSec * 0.45f, 0.13f), o + 4 * step); }
                    }
                    if (s.arp)
                    {
                        int[] ar = { ch[0] + 24, ch[2] + 24, ch[1] + 24 + 12, ch[3] + 24, ch[2] + 24, ch[1] + 24, ch[3] + 24, ch[2] + 36 };
                        for (int k = 0; k < 8; k++) AddWrap(bells, Glass(Hz(ar[k]), 0.5f, k % 2 == 0 ? 0.045f : 0.032f), o + k * step);
                    }
                    if (s.twinkle && rnd.NextDouble() < 0.8)
                        AddWrap(bells, Bell(Hz(ch[rnd.Next(4)] + 36), 1.4f, 0.035f), o + (rnd.Next(4) * 2 + 1) * step);
                }
                for (int k = 0; k < s.mel.Length; k++)
                {
                    int o = at + k * step;
                    if (s.mel[k] > 0) AddWrap(bells, Bell(Hz(s.mel[k]), 2.2f, 0.12f), o);
                    char d = s.drums[k % s.drums.Length];
                    if (d == 'k') AddWrap(dry, kick, o); else if (d == 'b') AddWrap(dry, tom, o);
                    else if (d == 's') AddWrap(dry, shaker, o); else if (d == 't') AddWrap(dry, tick, o);
                }
                at += s.mel.Length * step;
            }
            // 몽환: 종 · 분산화음에 점4분 메아리 → 종 + 패드에 큰 잔향 (두 바퀴 = 이음새 없음)
            var echo = Echo(bells, 3f * step / SR, 0.42f, true);
            var bus = new float[n];
            for (int i = 0; i < n; i++) bus[i] = bells[i] + echo[i] * 0.55f + pads[i];
            var wet = Reverb(bus, 1f, true);
            var outp = new float[n];
            for (int i = 0; i < n; i++) outp[i] = dry[i] + bus[i] * 0.75f + wet[i] * 0.9f;
            // 크기 맞춤 (곡마다 RMS 목표 — tools/sound_review.py 로 체감 크기 확인)
            double sum = 0; for (int i = 0; i < n; i++) sum += outp[i] * outp[i];
            float rms = (float)Math.Sqrt(sum / n), g = rms > 0 ? target / rms : 1f;
            for (int i = 0; i < n; i++) outp[i] = Mathf.Clamp(outp[i] * g, -0.98f, 0.98f);
            return outp;
        }
    }
}
