using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using AngryBirds;
using static UnityEngine.ParticleSystem;

public class ModAPI2
{
    public static string metaString;

    public void setMetaString(string st)
    {
        metaString = st;
    }

    public static Sprite LoadSprite(string path, float scale = 1f)
    {
        string str = Path.Combine(metaString, path);
        Sprite sprite1;
        if (ModResourceCache.TryGet<Sprite>(str, out sprite1))
        {
            if(sprite1 != null)
                return sprite1;
        }
        Sprite sprite2 = Utils.LoadSprite(str, true ? FilterMode.Point : FilterMode.Bilinear, scale * 35f);
        ModResourceCache.Cache(str, (UnityEngine.Object)sprite2);
        if(sprite2 != null)
            return sprite2;
        return null;
    }
    public static AudioClip LoadSound(string path)
    {
        string str = Path.Combine(metaString, path);
        AudioClip audioClip1;
        if (ModResourceCache.TryGet<AudioClip>(str, out audioClip1))
        {
            if(audioClip1 != null)
                return audioClip1;
        }
        AudioClip audioClip2 = Utils.FileToAudioClip(str);
        ModResourceCache.Cache(str, (UnityEngine.Object)audioClip2);
        if(audioClip2 != null)
            return audioClip2;
        return null;
    }
}
public static class SoundFX
{
    static Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    static HashSet<string> warned = new HashSet<string>();
 
    // file = just the file name, e.g. "shockwave.wav" (it's looked for in the AudioClip folder)
    public static void PlayAt(string file, Vector3 position, float volume)
    {
        AudioClip clip = null;
        if (!cache.TryGetValue(file, out clip) || clip == null)
        {
            clip = ModAPI2.LoadSound("AudioClip/" + file);
            if (clip != null && clip.length > 0.01f)
                cache[file] = clip;
            else
                clip = null;
        }
        if (clip == null)
        {
            if (warned.Add(file))
                ModAPI.Notify("<color=red>" + file + " didn't load</color>");
            return;
        }
 
        GameObject g = new GameObject("SFX " + file);
        g.transform.position = position;
        AudioSource s = g.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = false;
        s.mute = false;
        s.volume = Mathf.Clamp01(volume);
        s.maxDistance = 100f;
        s.rolloffMode = AudioRolloffMode.Linear;
        s.spatialBlend = 1f;
        s.PlayOneShot(clip);
        UnityEngine.Object.Destroy(g, clip.length + 0.5f);
    }
}
public class Red : MonoBehaviour
{
    AngryBird bird;
    AudioSource cawSource;
    AudioClip[] caws = { null, null, null, null };
 
    void Start()
    {
        bird = GetComponent<AngryBird>();
        if (bird != null)
            bird.noAbility = true;      // so the trail doesn't mark an "ability" cloud when he caws
 
        // His own AudioSource, 2D so distance and the listener can't make it inaudible
        cawSource = gameObject.AddComponent<AudioSource>();
        cawSource.playOnAwake = false;
        cawSource.loop = false;
        cawSource.mute = false;
        cawSource.volume = 1f;
        cawSource.spatialBlend = 0f;
 
        try
        {
            for (int n = 1; n <= 3; n++)
                caws[n] = LoadCaw(n);
        }
        catch (System.Exception e)
        {
            ModAPI.Notify("<color=red>Red: loading the caws failed: " + e.Message + "</color>");   // DEBUG
        }
 
        int loaded = 0;
        for (int n = 1; n <= 3; n++)
            if (caws[n] != null)
                loaded++;
 
        if (!gameObject.GetComponent<TriggerEventBehaviour>())
        {
            gameObject.AddComponent<UseEventTrigger>().Action = () =>
            {
                Caw();
            };
        }
        else
            ModAPI.Notify("<color=red>Red: a use-trigger already exists, so Caw() isn't hooked up</color>");   // DEBUG
    }
 
    // Tries red_caw1.wav / red_caw2.wav / red_caw3.wav first, then red_caw.wav / red_caw_2.wav / red_caw_3.wav.
    // A missing or broken file can come back as null OR as an empty clip, so only a clip with real length counts.
    static AudioClip LoadCaw(int n)
    {
        string[] names;
        if (n == 1)
            names = new string[] { "red_caw1.wav", "red_caw.wav", "red_caw_1.wav" };
        else
            names = new string[] { "red_caw" + n + ".wav", "red_caw_" + n + ".wav" };
 
        for (int i = 0; i < names.Length; i++)
        {
            AudioClip c = ModAPI2.LoadSound("AudioClip/" + names[i]);
            if (c != null && c.length > 0.01f)
                return c;
        }
        return null;
    }
 
    void Caw()
    {
        for (int n = 1; n <= 3; n++)
            if (caws[n] == null)
                caws[n] = LoadCaw(n);       // try again now, in case they weren't ready at spawn
 
        List<AudioClip> found = new List<AudioClip>();
        for (int i = 1; i <= 3; i++)
            if (caws[i] != null)
                found.Add(caws[i]);
        if (found.Count == 0)
        {
            ModAPI.Notify("<color=red>Red: none of the red_caw clips loaded</color>");
            return;
        }
 
        AudioClip pick = found[UnityEngine.Random.Range(0, found.Count)];
        cawSource.PlayOneShot(pick, 1f);
    }
}
public class MightyRed : MonoBehaviour
{
    AngryBird bird;
    bool active = false;
 
    // Tuning knobs
    public float pauseTime = 1f;      // seconds he hangs in the air before dashing
    public float dashSpeed = 48f;     // launch speed (Chuck's dash is about 30)
    public float dashDuration = 1f;   // how long he stays heavy / destructive after launching
	// Tuning knobs for the feathers
    public int featherCount = 6;
    public float featherRadius = 1.1f;
    public float featherSpin = 120f;
    public bool onlyWhileCharging = false;   // true = feathers only appear during the pause + dash
	public bool regrowFeathers = true;   // new feathers appear again once the dash ends
    // Only for Slingshot X shots
    public float backDistance = 1f;     // how far he backs away from the mouse before dashing
    public float turnSpeed = 440f;       // degrees per second when turning to face the mouse
    public bool facesRight = true;        // set to false if his sprite faces LEFT and he turns the wrong way
    public bool dashUntilCollision = true;   // keeps dashing (zero gravity, heavy) until he collides with something
    public float maxDashTime = 8f;           // safety limit, in case he never hits anything
    bool hitSomething = false;
    public float dashSpin = 1080f;            // degrees per second he spins while dashing (0 = no spin)
 
    List<GameObject> feathers = new List<GameObject>();
 
    void SpawnFeathers()
    {
        if (feathers.Count > 0)
            return;
        for (int i = 0; i < featherCount; i++)
        {
            GameObject f = new GameObject("OrbitFeather");
            OrbitFeather o = f.AddComponent<OrbitFeather>();
            o.owner = transform;
            o.color = (i % 2 == 0) ? (Color)new Color32(220, 30, 30, 255) : Color.white;
            o.radius = featherRadius;
            o.speed = featherSpin;
            o.angle = 360f / featherCount * i;
            feathers.Add(f);
        }
    }
 
    void DropFeathers()
    {
        foreach (GameObject f in feathers)
        {
            if (f == null)
                continue;
            OrbitFeather o = f.GetComponent<OrbitFeather>();
            if (o != null)
                o.Drop(bird.rb.velocity * 0.3f);   // they keep a bit of his speed so they trail behind
        }
        feathers.Clear();   // from here on the dropped feathers manage (and delete) themselves
    }
// (OnDestroy stays as it was.)
 
    void OnDestroy()
    {
        foreach (GameObject f in feathers)
            if (f != null)
                Destroy(f);
    }

 
    void Start()
    {
        bird = GetComponent<AngryBird>();
		if (!onlyWhileCharging) SpawnFeathers();
 
        // Same guard the other birds use, so the ability isn't hooked up twice.
        if (!gameObject.GetComponent<TriggerEventBehaviour>())
        {
            gameObject.AddComponent<UseEventTrigger>().Action = () =>
            {
                if (active)
                    return;
                StartCoroutine(Dash());
            };
        }
    }
	
    // The z angle that makes him face a direction
    float AngleOf(Vector2 dir)
    {
        float a = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        bool flipped = transform.localScale.x < 0f;
        if (facesRight == flipped)
            a += 180f;
        return a;
    }
 
     void OnCollisionEnter2D(Collision2D c)
    {
        if (active)
            hitSomething = true;
    }
 
    IEnumerator Dash()
    {
        active = true;
        bird.freeRotation = true;
        bool fromSlingshot = bird.shotFromSlingshot;     // fired from Slingshot X? then he behaves a bit differently
        if (onlyWhileCharging) SpawnFeathers();
        float originalMass = bird.rb.mass;
        float originalGravity = bird.rb.gravityScale;
 
        // Slingshot shots: the aim is locked the second you activate him
        Vector2 lockedDir = Vector2.right;
        if (fromSlingshot)
        {
            Vector2 mouse = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
            lockedDir = (mouse - (Vector2)transform.position).normalized;
            if (lockedDir == Vector2.zero)
                lockedDir = Vector2.right;
        }
 
        // ---- Phase 1: hover (slingshot shots: turn to the mouse and back away) ----
        bird.enableSelfBalancing = false;   // stop the "stand upright" torque while charging
        bird.rb.gravityScale = 0f;
        bird.mouth.clip = bird.use[UnityEngine.Random.Range(1, 4)];
        bird.mouth.Play();
        Utility.CreateFeatherEffect(new Color32(255, 0, 0, 255), transform.position);
 
        float t = 0f;
        while (t < pauseTime)
        {
            if (fromSlingshot)
            {
                // turn toward the mouse, and drift backwards (fast at first, then slowing to a stop)
                float backSpeed = 2f * backDistance / Mathf.Max(0.01f, pauseTime) * (1f - t / pauseTime);
                bird.rb.velocity = -lockedDir * backSpeed;
                float next = Mathf.MoveTowardsAngle(transform.eulerAngles.z, AngleOf(lockedDir), turnSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0f, 0f, next);
            }
            else
                bird.rb.velocity = Vector2.zero;
            bird.rb.angularVelocity = 0f;
            t += Time.deltaTime;
            yield return null;
        }
 
        // ---- Phase 2: launch ----
        Vector2 dir;
        if (fromSlingshot)
        {
            dir = lockedDir;                          // the direction locked at activation
        }
        else
        {
            // not from the slingshot: aim at the mouse RIGHT NOW (after the pause), as before
            Vector2 aimPoint = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (bird.AI && bird.target != null)          // with AI on, dash at his target instead
                aimPoint = bird.target.transform.position;
            dir = (aimPoint - (Vector2)transform.position).normalized;
            if (dir == Vector2.zero)
                dir = Vector2.right;
        }
 
        bird.wireAction = AngryBird.WireAction.DestroyOnHighSpeed;
        bird.rb.mass = originalMass * 3f;            // heavier = hits harder (same trick as Chuck/Hal)
        bird.rb.velocity = dir * dashSpeed;
        float spinSign = dir.x >= 0f ? -1f : 1f;                 // rolls forward: clockwise when going right, anticlockwise when going left
        bird.rb.angularVelocity = dashSpin * spinSign;
        DropFeathers();
        ModAPI.CreateParticleEffect("Flash", transform.position);
 
        if (dashUntilCollision)
        {
            float flown = 0f;
            while (!hitSomething && flown < maxDashTime)
            {
                flown += Time.deltaTime;
                bird.rb.angularVelocity = dashSpin * spinSign;
                yield return null;
            }
        }
        else
            yield return new WaitForSeconds(dashDuration);
 
        // ---- Phase 3: back to normal ----
        bird.rb.mass = originalMass;
        bird.rb.gravityScale = originalGravity;
        bird.freeRotation = false;
        // slingshot shots don't grow their feathers back
        if (regrowFeathers && !onlyWhileCharging && !fromSlingshot) SpawnFeathers();
        active = false;
    }
}
public class Telebird : MonoBehaviour
{
    AngryBird bird;
    bool busy = false;
 
    // Tuning knobs
    public float vortexTime = 1.5f;      // seconds between vanishing and reappearing ("a second or two")
    public float vortexRadius = 1.5f;    // size of the vortex
    public Color32 vortexColor = new Color32(170, 80, 255, 255);
 
    void Start()
    {
        bird = GetComponent<AngryBird>();
 
        if (!gameObject.GetComponent<TriggerEventBehaviour>())
        {
            gameObject.AddComponent<UseEventTrigger>().Action = () =>
            {
                if (busy)
                    return;
                StartCoroutine(Teleport());
            };
        }
    }
 
    IEnumerator Teleport()
    {
        busy = true;
 
        // Remember everything that has to survive the trip
        Vector2 velocity = bird.rb.velocity;
        float spin = bird.rb.angularVelocity;
        Vector3 originalScale = transform.localScale;
        Quaternion originalRot = transform.rotation;
 
        // Same ability voice Red uses
        bird.mouth.clip = bird.use[UnityEngine.Random.Range(1, 4)];
        bird.mouth.Play();
        Utility.DestroyWires(gameObject);   // so nothing yanks him back when he leaves
 
        // ---- Phase 1: get sucked into the vortex (spin + shrink), then vanish ----
        TeleVortex.Create(transform.position, vortexRadius, vortexTime, vortexColor);
        bird.rb.simulated = false;          // frozen out of physics while he's "gone"
 
        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            transform.localScale = originalScale * Mathf.Max(0.01f, 1f - t / 0.25f);   // keeps the +/- sign used for facing
            transform.Rotate(0f, 0f, 720f * Time.deltaTime);
            yield return null;
        }
 
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        bool[] wasOn = new bool[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            wasOn[i] = renderers[i].enabled;
            renderers[i].enabled = false;
        }
 
        // ---- Phase 2: gone ----
        yield return new WaitForSeconds(Mathf.Max(0f, vortexTime - 0.25f));
 
        // ---- Phase 3: reappear at the mouse, with all his momentum ----
        Vector2 target = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);   // read now, so you can move the mouse while he's gone
        TeleVortex.Create(target, vortexRadius, 0.7f, vortexColor);
        ModAPI.CreateParticleEffect("Flash", target);
 
        transform.position = new Vector3(target.x, target.y, transform.position.z);
        transform.rotation = originalRot;
        transform.localScale = originalScale;
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null)
                renderers[i].enabled = wasOn[i];
 
        bird.rb.simulated = true;
        bird.rb.velocity = velocity;        // same speed and direction as when he left
        bird.rb.angularVelocity = spin;     // same spin too
 
        busy = false;
    }
}
 
// The vortex: a few rotating spiral arms drawn with LineRenderers. No art needed.
// Grows in over 0.25s, spins, shrinks away over the last 0.3s, then deletes itself.
public class TeleVortex : MonoBehaviour
{
    public float radius = 1.5f;
    public float lifetime = 1.5f;
    public Color32 color = new Color32(170, 80, 255, 255);
    public float spin = 40f;       // degrees per second
    public float turns = 1f;     // how tightly wound the spiral is
 
    const int Arms = 3;
    const int Points = 24;
 
    LineRenderer[] lines;
    Material mat;
    float age = 0f;
 
    public static TeleVortex Create(Vector2 pos, float radius, float lifetime, Color32 color)
    {
        GameObject g = new GameObject("TeleVortex");
        g.transform.position = pos;
        TeleVortex v = g.AddComponent<TeleVortex>();
        v.radius = radius;
        v.lifetime = lifetime;
        v.color = color;
        return v;
    }
 
    void Start()
    {
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null)
        {
            Destroy(gameObject, lifetime);   // can't draw it, but still clean up
            return;
        }
        mat = new Material(sh);
 
        lines = new LineRenderer[Arms];
        for (int a = 0; a < Arms; a++)
        {
            GameObject g = new GameObject("VortexArm");
            g.transform.SetParent(transform, false);
            LineRenderer lr = g.AddComponent<LineRenderer>();
            lr.material = mat;
            lr.useWorldSpace = true;
            lr.positionCount = Points;
            lr.sortingOrder = 40;
            lr.startWidth = 0.03f;    // centre of the spiral
            lr.endWidth = 0.12f;      // outer tip
            Color c = (a % 2 == 0) ? (Color)color : Color.Lerp((Color)color, Color.white, 0.5f);
            lr.startColor = c;
            lr.endColor = c;
            lines[a] = lr;
        }
    }
 
    void Update()
    {
        if (lines == null)
            return;
 
        age += Time.deltaTime;
        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }
 
        float grow = Mathf.Clamp01(age / 0.25f);
        float shrink = Mathf.Clamp01((lifetime - age) / 0.3f);
        float s = Mathf.Min(grow, shrink);                 // 0 -> 1 -> 0
        float spinAngle = age * spin * Mathf.Deg2Rad;
        Vector3 c = transform.position;
 
        for (int a = 0; a < Arms; a++)
        {
            for (int p = 0; p < Points; p++)
            {
                float t = p / (Points - 1f);
                float ang = a * (Mathf.PI * 2f / Arms) + t * turns * Mathf.PI * 2f + spinAngle;
                float r = radius * s * t;
                lines[a].SetPosition(p, new Vector3(c.x + Mathf.Cos(ang) * r, c.y + Mathf.Sin(ang) * r, c.z));
            }
        }
    }
 
    void OnDestroy()
    {
        if (mat != null)
            Destroy(mat);
    }
}
// ===== Paste both classes into AngryBirdsBehaviour.cs (anywhere, e.g. right after the Telebird class) =====
 
public class HomingBird : MonoBehaviour
{
    AngryBird bird;
    HomingField field;
    bool busy = false;
    float cooldownTimer = 0f;
    float scanTimer = 0f;
 
    // Tuning knobs
    public float fieldRadius = 8f;       // size of the green field
    public float lockTime = 1f;        // seconds he hangs in the air turning toward the pig
    public float turnSpeed = 480f;       // degrees per second while turning
    public float dashSpeed = 48f;        // launch speed of the dash (Mighty Feathers Red uses 48)
    public float dashDuration = 0.5f;    // how long he stays heavy / destructive after launching
    public float cooldown = 20f;          // seconds after a dash before he can lock on again
    public bool onlyInFlight = true;    // true = only homes while flying from the Slingshot X (not while waiting on the ground)
    public bool facesRight = true;       // set to false if his sprite faces LEFT and he points the wrong way
 
    void Start()
    {
        bird = GetComponent<AngryBird>();
        field = HomingField.Create(transform, fieldRadius);
    }
 
    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
 
        scanTimer -= Time.deltaTime;
        if (scanTimer > 0f)
            return;
        scanTimer = 0.1f;
 
        BadPig pig = FindPigInField();
 
        // The field is red while a pig is inside it (and while he is striking)
        if (field != null)
            field.danger = busy || pig != null;
 
        if (busy || cooldownTimer > 0f || pig == null)
            return;
        if (bird == null || bird.rb == null || !bird.rb.simulated)   // not while he's held in the slingshot
            return;
        if (onlyInFlight && bird.stat != AngryBird.Status.SlingFly)
            return;
        if (bird != null && bird.AbilityLocked)
            return;
 
        StartCoroutine(Strike(pig));
    }
 
    BadPig FindPigInField()
    {
        BadPig best = null;
        float bestDist = fieldRadius;
        foreach (BadPig p in FindObjectsOfType<BadPig>())
        {
            if (p == null || !p.gameObject.activeInHierarchy)
                continue;
            float d = Utility.Distance((Vector2)p.transform.position, (Vector2)transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = p;
            }
        }
        return best;
    }
 
    // The z angle that makes him face the given point
    float AngleTo(Vector2 point)
    {
        Vector2 d = point - (Vector2)transform.position;
        float a = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        bool flipped = transform.localScale.x < 0f;
        if (facesRight == flipped)
            a += 180f;
        return a;
    }
 
    IEnumerator Strike(BadPig pig)
    {
        busy = true;
		BirdTrail homingTrail = GetComponent<BirdTrail>();
        if (homingTrail != null)
            homingTrail.MarkAbility();
        float originalMass = bird.rb.mass;
        float originalGravity = bird.rb.gravityScale;
 
        // ---- Phase 1: stop mid-air and turn toward the pig ----
        bird.enableSelfBalancing = false;
        bird.rb.gravityScale = 0f;
        bird.mouth.clip = bird.use[UnityEngine.Random.Range(1, 4)];
        bird.mouth.Play();
        Utility.CreateFeatherEffect(new Color32(255, 40, 40, 255), transform.position);
 
        float t = 0f;
        while (t < lockTime && pig != null)
        {
            bird.rb.velocity = Vector2.zero;
            bird.rb.angularVelocity = 0f;
            float current = transform.eulerAngles.z;
            float next = Mathf.MoveTowardsAngle(current, AngleTo((Vector2)pig.transform.position), turnSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, 0f, next);
            t += Time.deltaTime;
            yield return null;
        }
 
        // ---- Phase 2: dash at the pig (if it's still there) ----
        if (pig != null)
        {
            Vector2 dir = ((Vector2)pig.transform.position - (Vector2)transform.position).normalized;
            if (dir == Vector2.zero)
                dir = Vector2.right;
 
            bird.wireAction = AngryBird.WireAction.DestroyOnHighSpeed;
            bird.rb.mass = originalMass * 3f;            // heavier = hits harder
            bird.rb.velocity = dir * dashSpeed;
            bird.rb.angularVelocity = 0f;
            ModAPI.CreateParticleEffect("Flash", transform.position);
 
            float d = 0f;
            while (d < dashDuration)
            {
                bird.rb.angularVelocity = 0f;            // keep him pointing at where he's going
                d += Time.deltaTime;
                yield return null;
            }
        }
 
        // ---- Phase 3: back to normal ----
        bird.rb.mass = originalMass;
        bird.rb.gravityScale = originalGravity;
        bird.enableSelfBalancing = true;
        cooldownTimer = cooldown;
        busy = false;
    }
}
 
// The field: a faint filled disc plus a ring, following the bird. Green normally, red when danger is true.
public class HomingField : MonoBehaviour
{
    public Transform owner;
    public float radius = 5f;
    public bool danger = false;
 
    const int Points = 48;
    static Sprite discSprite = null;
 
    SpriteRenderer fill;
    LineRenderer ring;
    Color current = new Color(0.2f, 1f, 0.3f, 1f);
    Color safeColor = new Color(0.2f, 1f, 0.3f, 1f);
    Color dangerColor = new Color(1f, 0.15f, 0.1f, 1f);
 
    public static HomingField Create(Transform owner, float radius)
    {
        GameObject g = new GameObject("HomingField");
        g.transform.position = owner.position;
        HomingField f = g.AddComponent<HomingField>();
        f.owner = owner;
        f.radius = radius;
        return f;
    }
 
    void Start()
    {
        int order = 0;
        SpriteRenderer osr = owner != null ? owner.GetComponent<SpriteRenderer>() : null;
        if (osr != null)
            order = osr.sortingOrder - 1;
 
        // Faint filled disc (child, so it can be scaled on its own)
        GameObject f = new GameObject("HomingFieldFill");
        f.transform.SetParent(transform, false);
        f.transform.localScale = Vector3.one * radius * 2f;
        fill = f.AddComponent<SpriteRenderer>();
        fill.sprite = GetDiscSprite();
        fill.sortingOrder = order;
 
        // Ring
        Shader sh = Shader.Find("Sprites/Default");
        if (sh != null)
        {
            ring = gameObject.AddComponent<LineRenderer>();
            ring.material = new Material(sh);
            ring.useWorldSpace = false;
            ring.positionCount = Points + 1;
            ring.startWidth = 0.08f;
            ring.endWidth = 0.08f;
            ring.sortingOrder = order;
            for (int i = 0; i <= Points; i++)
            {
                float a = (float)i / Points * Mathf.PI * 2f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
            }
        }
    }
 
    void Update()
    {
        if (owner == null)
        {
            Destroy(gameObject);
            return;
        }
        transform.position = owner.position;
        transform.rotation = Quaternion.identity;
 
        Color target = danger ? dangerColor : safeColor;
        current = Color.Lerp(current, target, Mathf.Min(1f, 12f * Time.deltaTime));
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * (danger ? 10f : 3f));
 
        if (fill != null)
            fill.color = new Color(current.r, current.g, current.b, 0.10f + 0.08f * pulse);
        if (ring != null)
        {
            Color rc = new Color(current.r, current.g, current.b, 0.55f + 0.35f * pulse);
            ring.startColor = rc;
            ring.endColor = rc;
        }
    }
 
    // A round disc, generated in code so no extra image is needed (1 unit across)
    static Sprite GetDiscSprite()
    {
        if (discSprite != null)
            return discSprite;
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float c = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                float alpha = Mathf.Clamp01(size / 2f - dist);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        discSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return discSprite;
    }
}
// Makes a bird fly in zero gravity once it is shot from Slingshot X.
// (Both Space birds add this to themselves; later Space birds can do the same.)
public class SpaceFlight : MonoBehaviour
{
    AngryBird bird;
    bool applied = false;
    float originalGravity = 1f;
    float flightTime = 0f;
    float hitTime = 0f;
    bool done = false;
 
    // Tuning knobs
    public bool zeroGravityOnLaunch = true;   // false = this bird flies like a normal bird
    public float maxFlightTime = 8f;         // a bird that never hits anything disappears this many seconds after launch
    public float despawnAfterHit = 14f;        // a bird drifting around after its first hit disappears after this many seconds
 
    void Start()
    {
        bird = GetComponent<AngryBird>();
    }
 
    void FixedUpdate()
    {
        if (done || bird == null || bird.rb == null)
            return;
 
        if (bird.shotFromSlingshot)
        {
            if (!applied)
            {
                // The moment he is launched: remember normal gravity, then switch it off
                applied = true;
                originalGravity = bird.rb.gravityScale;
                flightTime = 0f;
                hitTime = 0f;
                if (zeroGravityOnLaunch)
                    bird.rb.gravityScale = 0f;
            }
 
            flightTime += Time.fixedDeltaTime;
            if (bird.hasCollidedAfterShot)
                hitTime += Time.fixedDeltaTime;
 
            // Without gravity he would drift forever (the "lying still" despawn never triggers), so he gets a time limit
            if (flightTime > maxFlightTime || hitTime > despawnAfterHit)
            {
                done = true;
                bird.Die();
            }
        }
        else if (applied)
        {
            // He was caught by the slingshot again: back to normal gravity
            applied = false;
            bird.rb.gravityScale = originalGravity;
        }
    }
}
 
// The red ring that expands outward from Super Red's shockwave
public class SpaceWaveRing : MonoBehaviour
{
    public float maxRadius = 8f;
    public float duration = 0.45f;
    public float width = 0.25f;
    public Color32 color = new Color32(255, 40, 40, 255);
 
    const int Points = 48;
    LineRenderer lr;
    float t = 0f;
 
    void Start()
    {
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null)
        {
            Destroy(gameObject);
            return;
        }
        lr = gameObject.AddComponent<LineRenderer>();
        lr.material = new Material(sh);
        lr.useWorldSpace = true;
        lr.positionCount = Points + 1;      // one extra point closes the circle
        lr.sortingOrder = 50;
    }
 
    void Update()
    {
        if (lr == null)
            return;
 
        t += Time.deltaTime;
        float f = Mathf.Clamp01(t / duration);
        float ease = 1f - (1f - f) * (1f - f);          // fast at first, slowing toward the edge
        float r = Mathf.Lerp(0.3f, maxRadius, ease);
 
        Color c = color;
        c.a = 1f - f;
        lr.startColor = c;
        lr.endColor = c;
        lr.startWidth = width * (1f - 0.6f * f);
        lr.endWidth = width * (1f - 0.6f * f);
 
        Vector3 center = transform.position;
        for (int i = 0; i <= Points; i++)
        {
            float a = (float)i / Points * 2f * Mathf.PI;
            lr.SetPosition(i, new Vector3(center.x + Mathf.Cos(a) * r, center.y + Mathf.Sin(a) * r, center.z));
        }
 
        if (f >= 1f)
            Destroy(gameObject);
    }
}
 
// SUPER RED: when activated he sends out a red shockwave that pushes everything away (a bit)
public class SuperRed : MonoBehaviour
{
    AngryBird bird;
    bool ready = true;
 
    // Tuning knobs
    public float radius = 4f;            // how far the wave reaches
    public float power = 8f;            // push speed at the centre (fades toward the edge). Bigger = stronger push
    public float massThreshold = 40f;    // bodies lighter than this get the full push; heavier ones get proportionally less
    public float cooldown = 0f;          // seconds before he can fire again
    public bool pushOtherBirds = true;  // false = friendly birds ("Player" tag) are left alone
    public float effectScale = 1f;     // multiplies the size of the explosion / flash effect
    public Color32 waveColor = new Color32(255, 0, 0, 255);
 
    void Start()
    {
        bird = GetComponent<AngryBird>();
 
        // Zero gravity when shot from Slingshot X
        if (!GetComponent<SpaceFlight>())
            gameObject.AddComponent<SpaceFlight>();
 
        // Same guard the other birds use, so the ability isn't hooked up twice
        if (!gameObject.GetComponent<TriggerEventBehaviour>())
        {
            gameObject.AddComponent<UseEventTrigger>().Action = () =>
            {
                if (!ready)
                    return;
                ready = false;
                Invoke("Recharge", cooldown);
                BirdTrail trail = GetComponent<BirdTrail>();
                if (trail != null)
                    trail.MarkAbility();
                Blast();
            };
        }
    }
 
    void Recharge()
    {
        ready = true;
    }
 
    // Says a random one of his ability voicelines (only the ones that exist)
    void SayAbility()
    {
        if (bird == null || bird.mouth == null || bird.use == null)
            return;
        List<AudioClip> clips = new List<AudioClip>();
        for (int i = 1; i < bird.use.Length && i <= 3; i++)
            if (bird.use[i] != null)
                clips.Add(bird.use[i]);
        if (clips.Count == 0)
            return;
        bird.mouth.clip = clips[UnityEngine.Random.Range(0, clips.Count)];
        bird.mouth.Play();
    }
 
    void Blast()
    {
        SayAbility();
        Utility.CreateFeatherEffect(new Color32(255, 0, 0, 255), transform.position);
 
        // Visuals: a red explosion, a flash, and a ring that spreads out to the edge of the wave
        Utility.CreateAnimation("Explosion", 15, transform.position, waveColor).transform.localScale = 2f * effectScale * Vector2.one;
        ModAPI.CreateParticleEffect("Flash", transform.position).transform.localScale = effectScale * Vector3.one;
        GameObject ringObject = new GameObject("SpaceWaveRing");
        ringObject.transform.position = transform.position;
        SpaceWaveRing ring = ringObject.AddComponent<SpaceWaveRing>();
        ring.maxRadius = radius;
        ring.color = waveColor;
 
        // One body can have several colliders, so remember who we already pushed
        HashSet<Rigidbody2D> done = new HashSet<Rigidbody2D>();
 
        foreach (Collider2D c in Physics2D.OverlapCircleAll(transform.position, radius))
        {
            Rigidbody2D target = c.attachedRigidbody;
            if (target == null || target == bird.rb || !done.Add(target))
                continue;
            if (target.gameObject.layer == 11)                       // the ground stays put
                continue;
            if (!pushOtherBirds && target.gameObject.CompareTag("Player"))
                continue;
 
            Vector2 offset = target.position - (Vector2)transform.position;
            Vector2 dir = offset.sqrMagnitude > 0.0001f ? offset.normalized : Vector2.up;
            float falloff = Mathf.Lerp(1f, 0.3f, offset.magnitude / radius);
 
            Utility.AddReasonableForceImpulse(target, dir, power * falloff, massThreshold);
        }
    }
}
public class Chuck : MonoBehaviour
{
    AngryBird bird;
    void Start()
    {
        bird = GetComponent<AngryBird>();
        bird.sprite[2] = ModAPI2.LoadSprite("byungsin.png");
        if (!gameObject.GetComponent<TriggerEventBehaviour>())
        {
            gameObject.AddComponent<UseEventTrigger>().Action = () =>
            {
                bird.wireAction = AngryBird.WireAction.DestroyOnHighSpeed;
                bird.rb.mass = bird.gameMass * 3;
                if (transform.localScale.x > 0)
                    bird.rb.AddForce(transform.right * bird.rb.mass * 30, ForceMode2D.Impulse);
                else
                    bird.rb.AddForce(transform.right * -bird.rb.mass * 30, ForceMode2D.Impulse);
                bird.mouth.clip = bird.use[UnityEngine.Random.Range(1, 4)];
                bird.mouth.Play();
                GetComponent<SpriteRenderer>().sprite = bird.sprite[2];
                bird.timer = 5f;
                ModAPI.CreateParticleEffect("Flash", transform.position);
                Invoke("Reset", 1);
            };
        }
    }
    void Reset()
    {
        bird.rb.mass = bird.mass;
    }
}
// LAZER BIRD: when activated he instantly shoots toward the mouse (turning to face it) and keeps going until he hits something
public class LazerBird : MonoBehaviour
{
    AngryBird bird;
    bool active = false;
    bool hitSomething = false;
    float activeTime = 0f;
 
    // Tuning knobs
    public float dashSpeed = 40f;          // how fast the laser dash is (Chuck's charge is about 30)
    public float massMultiplier = 3f;      // heavier while dashing = hits harder (same trick as Chuck)
    public float maxDashTime = 4f;         // safety limit, in case he never hits anything
    public float collisionGrace = 0.1f;   // seconds after activation in which touching something doesn't count
    public bool facesRight = true;         // set to false if his sprite faces LEFT and he turns the wrong way
 
    void Start()
    {
        bird = GetComponent<AngryBird>();
 
        // Zero gravity when shot from Slingshot X
        if (!GetComponent<SpaceFlight>())
            gameObject.AddComponent<SpaceFlight>();
 
        // Same guard the other birds use, so the ability isn't hooked up twice
        if (!gameObject.GetComponent<TriggerEventBehaviour>())
        {
            gameObject.AddComponent<UseEventTrigger>().Action = () =>
            {
                if (active)
                    return;
                BirdTrail trail = GetComponent<BirdTrail>();
                if (trail != null)
                    trail.MarkAbility();
                StartCoroutine(Zap());
            };
        }
    }
 
    // The z angle that makes him face a direction (same idea as Mighty Feathers Red and Silver)
    float AngleOf(Vector2 dir)
    {
        float a = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        bool flipped = transform.localScale.x < 0f;
        if (facesRight == flipped)
            a += 180f;
        return a;
    }
 
    void OnCollisionEnter2D(Collision2D c)
    {
        if (active && activeTime >= collisionGrace)
            hitSomething = true;
    }
 
    // Says a random one of his ability voicelines (only the ones that exist)
    void SayAbility()
    {
        if (bird == null || bird.mouth == null || bird.use == null)
            return;
        List<AudioClip> clips = new List<AudioClip>();
        for (int i = 1; i < bird.use.Length && i <= 3; i++)
            if (bird.use[i] != null)
                clips.Add(bird.use[i]);
        if (clips.Count == 0)
            return;
        bird.mouth.clip = clips[UnityEngine.Random.Range(0, clips.Count)];
        bird.mouth.Play();
    }
 
    IEnumerator Zap()
    {
        active = true;
        hitSomething = false;
        activeTime = 0f;
        bird.freeRotation = true;             // so the bird's own "point along the flight" rule doesn't fight the turn
        bird.enableSelfBalancing = false;
        float originalGravity = bird.rb.gravityScale;
        float originalMass = bird.rb.mass;
        AngryBird.WireAction originalWire = bird.wireAction;
 
        try
        {
            // Aim at the mouse RIGHT NOW (with AI on, at his target instead)
            Vector2 aimPoint = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (bird.AI && bird.target != null)
                aimPoint = bird.target.transform.position;
            Vector2 dir = (aimPoint - (Vector2)transform.position).normalized;
            if (dir == Vector2.zero)
                dir = Vector2.right;
 
            SayAbility();
            ModAPI.CreateParticleEffect("Flash", transform.position);
 
            bird.wireAction = AngryBird.WireAction.DestroyOnHighSpeed;
            bird.rb.mass = originalMass * massMultiplier;
            bird.rb.gravityScale = 0f;        // a laser flies straight
 
            // Turn and shoot in the very same moment
            bird.rb.rotation = AngleOf(dir);
            bird.rb.velocity = dir * dashSpeed;
            bird.rb.angularVelocity = 0f;
 
            float flown = 0f;
            while (!hitSomething && flown < maxDashTime)
            {
                bird.rb.velocity = dir * dashSpeed;
                bird.rb.angularVelocity = 0f;
                bird.rb.rotation = AngleOf(dir);
                flown += Time.fixedDeltaTime;
                activeTime += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
        }
        finally
        {
            // Back to normal (always, even if something interrupts the ability)
            if (bird != null && bird.rb != null)
            {
                bird.rb.mass = originalMass;
                bird.rb.gravityScale = originalGravity;
            }
            if (bird != null)
            {
                bird.freeRotation = false;
                bird.wireAction = originalWire;
            }
            active = false;
        }
    }
}
public class Bomb : MonoBehaviour
{
    bool activated = false;
    AngryBird bird;
    SpriteRenderer sr;
 
    // Fuse: after a Slingshot X shot hits something, Bomb slowly turns orange and then blows up
    public float fuseTime = 2.5f;                                   // seconds from the hit to the explosion
    public Color32 fuseColor = new Color32(255, 140, 0, 255);      // the tint he fades toward
    public bool vanishAfterExplosion = true;                        // Bomb disappears once he has exploded
 
    // Lighting up
    public Color32 glowColor = new Color32(255, 130, 0, 255);      // the bright orange of the glow
    public float glowSize = 4f;                                     // diameter of the halo around him (world units)
    public float overlayMaxAlpha = 0.8f;                           // how solid the orange gets at the end (1 = completely flat orange)
 
    // Blast damage
    public float blastRadius = 4f;      // how far his explosion damages things
    public float blockDamage = 28f;     // damage to a block right at the blast. Hp: glass 7, wood 14, stone 28, iron 56 (circles x2)
    public float pigDamage = 10f;        // pigs have 10 hp
 
    bool fusing = false;
    bool detonated = false;
    float fuseTimer = 0f;
    Color baseColor = Color.white;
 
    GameObject overlayObj = null;
    GameObject haloObj = null;
    SpriteRenderer overlaySr = null;
    SpriteRenderer haloSr = null;
    static Sprite haloSprite = null;
 
    void Start()
    {
        bird = GetComponent<AngryBird>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            baseColor = sr.color;
        gameObject.AddComponent<UseEventTrigger>().Action = () =>
        {
            Detonate();
        };
    }
 
    void Update()
    {
        if (detonated || bird == null || sr == null)
            return;
 
        bool shouldFuse = bird.shotFromSlingshot && bird.hasCollidedAfterShot;
        if (shouldFuse)
        {
            if (!fusing)
            {
                fusing = true;
                fuseTimer = 0f;
                baseColor = sr.color;
            }
            fuseTimer += Time.deltaTime;
            float k = Mathf.Clamp01(fuseTimer / fuseTime);
            sr.color = Color.Lerp(baseColor, fuseColor, k);
            SetGlow(k);
            if (fuseTimer >= fuseTime)
                Detonate();
        }
        else if (fusing)
        {
            // caught by the slingshot again: the fuse is cancelled
            fusing = false;
            sr.color = baseColor;
            HideGlow();
        }
    }
 
    // ---- The glow ----
    void EnsureGlow()
    {
        if (overlayObj != null)
            return;
 
        // bright orange copy of his sprite, drawn over him
        overlayObj = new GameObject("BombGlowOverlay");
        overlayObj.transform.SetParent(transform, false);
        overlaySr = overlayObj.AddComponent<SpriteRenderer>();
        overlaySr.sortingLayerID = sr.sortingLayerID;
        overlaySr.sortingOrder = sr.sortingOrder + 1;
 
        // soft round halo behind him
        haloObj = new GameObject("BombGlowHalo");
        haloObj.transform.SetParent(transform, false);
        haloSr = haloObj.AddComponent<SpriteRenderer>();
        haloSr.sprite = GetHaloSprite();
        haloSr.sortingLayerID = sr.sortingLayerID;
        haloSr.sortingOrder = sr.sortingOrder - 1;
        float parentScale = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.x));
        haloObj.transform.localScale = Vector3.one * glowSize / parentScale;      // glowSize in world units whatever his own scale is
    }
 
    // k: 0 = not glowing, 1 = about to explode
    void SetGlow(float k)
    {
        EnsureGlow();
        overlayObj.SetActive(true);
        haloObj.SetActive(true);
 
        overlaySr.sprite = sr.sprite;           // follows his sprite (it changes when he's stunned)
        overlaySr.flipX = sr.flipX;
        overlaySr.flipY = sr.flipY;
 
        Color gc = glowColor;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * (4f + 14f * k));     // pulses faster as the fuse runs out
        overlaySr.color = new Color(gc.r, gc.g, gc.b, k * overlayMaxAlpha * (0.7f + 0.3f * pulse));
        haloSr.color = new Color(gc.r, gc.g, gc.b, k * (0.35f + 0.35f * pulse));
    }
 
    void HideGlow()
    {
        if (overlayObj != null)
            overlayObj.SetActive(false);
        if (haloObj != null)
            haloObj.SetActive(false);
    }
 
    // A soft round halo, brightest in the middle, generated in code (1 unit across)
    static Sprite GetHaloSprite()
    {
        if (haloSprite != null)
            return haloSprite;
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float c = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / (size / 2f);
                float a = Mathf.Clamp01(1f - dist);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        haloSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return haloSprite;
    }
 
    // The explosion itself (used by the Activate key, X, and the fuse)
    void Detonate()
    {
        if (detonated)
            return;
        detonated = true;
        BirdTrail bombTrail = GetComponent<BirdTrail>();
        if (bombTrail != null)
            bombTrail.MarkAbility();
        bird.mouth.clip = bird.use[UnityEngine.Random.Range(1, 4)];
        bird.mouth.Play();
        GetComponent<ExplosiveBehaviour>().Activate();
        StartCoroutine("Activate");
    }
 
    IEnumerator Activate()
    {
        Utility.CreateAnimation("Explosion", 15, transform.position, new Color32(255,255,255,255)).transform.localScale = 2 * Vector2.one;
        BlastDamage.Apply(transform.position, blastRadius, blockDamage, pigDamage, gameObject);
        activated = true;
        GetComponent<CircleCollider2D>().enabled = false;
        GetComponent<CircleCollider2D>().enabled = true;
        yield return new WaitForSeconds(0.1f);
        activated = false;
 
        if (vanishAfterExplosion)
        {
            bird.Vanish(2.5f);          // gone right away (the voice has time to finish); the glow is hidden with him
        }
        else
        {
            // reusable Bomb: back to normal
            detonated = false;
            fusing = false;
            HideGlow();
            if (sr != null)
                sr.color = baseColor;
        }
    }
 
    void OnTriggerStay2D(Collider2D c)
    {
        if (!activated)
            return;
        if (c.gameObject.layer == 11)
            return;
        Utility.DestroyWires(c.gameObject);
    }
 
    void OnTriggerEnter2D(Collider2D c)
    {
        if (!activated)
            return;
        if (c.gameObject.layer == 11)
            return;
        if (c.gameObject.GetComponent<Rigidbody2D>())
            Utility.AddReasonableForceImpulse(c.gameObject.GetComponent<Rigidbody2D>(), c.transform.position - transform.position, 10, 5);
    }
}
public class Shockwave : MonoBehaviour
{
    AngryBird bird;
    bool ready = true;
 
    // Tuning knobs
    public float radius = 14f;          // how far the wave reaches
    public float power = 28f;           // launch speed at the centre (fades toward the edge)
    public float massThreshold = 84f;  // bodies lighter than this get the full speed boost;
                                        // heavier ones get proportionally less
    public float pigDamage = 44f;       // damage dealt to pigs (a minion has 8 hp, King has 60)
    public bool killPigs = true;       // true = every pig in range dies outright
    public float cooldown = 1f;         // seconds before he can fire again
    public float effectScale = 2.8f;    // multiplies the size of the visual effects
    public float shakeStrength = 248f;  // same number Terence uses
    public float shakeTime = 0.7f;      // how long the screen shakes (seconds)
 
    void Start()
    {
        bird = GetComponent<AngryBird>();
        if (!GetComponent<ShockwaveSparks>())
            gameObject.AddComponent<ShockwaveSparks>();
 
        if (!gameObject.GetComponent<TriggerEventBehaviour>())
        {
            gameObject.AddComponent<UseEventTrigger>().Action = () =>
            {
                if (!ready)
                    return;
                ready = false;
                Invoke("Recharge", cooldown);
                Blast();
            };
        }
    }
 
    void Recharge()
    {
        ready = true;
    }
 
    void Blast()
    {
        ShockwaveSparks sparks = GetComponent<ShockwaveSparks>();
        if (sparks != null)
            sparks.Burst();
		bird.mouth.clip = bird.use[UnityEngine.Random.Range(1, 4)];
        bird.mouth.Play();
        SoundFX.PlayAt("shockwave.wav", transform.position, 1f);
 
        // Visual: reuses the existing "Explosion" sprite sheet, tinted electric blue
        Utility.CreateAnimation("Explosion", 15, transform.position, new Color32(120, 220, 255, 255)).transform.localScale = 2f * effectScale * Vector2.one;
        ModAPI.CreateParticleEffect("Flash", transform.position).transform.localScale = effectScale * Vector3.one;
        StartCoroutine(ShakeScreen());
 
        // One body can have several colliders, so remember who we already hit
        HashSet<Rigidbody2D> done = new HashSet<Rigidbody2D>();
 
        foreach (Collider2D c in Physics2D.OverlapCircleAll(transform.position, radius))
        {
            Rigidbody2D target = c.attachedRigidbody;
            if (target == null || target == bird.rb || !done.Add(target))
                continue;
            if (target.gameObject.layer == 11)      // Bomb skips this layer too
                continue;
 
            Vector2 offset = target.position - (Vector2)transform.position;
            Vector2 dir = offset.sqrMagnitude > 0.0001f ? offset.normalized : Vector2.up;
            float falloff = Mathf.Lerp(1f, 0.4f, offset.magnitude / radius);
 
            Utility.DestroyWires(target.gameObject);                                   // snap ropes/struts like Bomb does
            Utility.AddReasonableForceImpulse(target, dir, power * falloff, massThreshold);
 
            BadPig pig = target.GetComponent<BadPig>();
            if (pig != null)
            {
                if (pig.isNormallyInvincible)       // El Porkador: knocked back, not hurt
                    pig.Stun(1f);
                else
                    pig.Stun(killPigs ? pig.hp * 2f + 1f : pigDamage);
            }
        }
    }
	
	    IEnumerator ShakeScreen()
    {
        float t = 0f;
        while (t < shakeTime)
        {
            CameraShakeBehaviour.main.Shake(shakeStrength, transform.position);
            t += Time.deltaTime;
            yield return null;
        }
    }
	
}
public class ShockwaveSparks : MonoBehaviour
{
    public int arcCount = 4;          // how many arcs can be visible at once
    public float radius = 1f;       // how far from his centre they reach
    public float width = 0.04f;       // line thickness
    public float burstScale = 4f;     // how much bigger/brighter it gets when he fires
 
    const int Points = 8;             // segments per arc (more = smoother, less jagged)
    LineRenderer[] lines;
    Vector2[][] offsets;              // each arc stored as offsets from his centre, so it follows him
    float timer = 0f;
    float burstTimer = 0f;
    Material mat;
 
    void Start()
    {
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null)
        {
            enabled = false;
            return;
        }
        mat = new Material(sh);
 
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        int order = (sr != null ? sr.sortingOrder : 0) + 2;   // draw in front of him
 
        lines = new LineRenderer[arcCount];
        offsets = new Vector2[arcCount][];
        for (int i = 0; i < arcCount; i++)
        {
            GameObject g = new GameObject("ShockArc");
            LineRenderer lr = g.AddComponent<LineRenderer>();
            lr.material = mat;
            lr.useWorldSpace = true;
            lr.positionCount = Points;
            lr.sortingOrder = order;
            lr.enabled = false;
            lines[i] = lr;
            offsets[i] = new Vector2[Points];
        }
    }
 
    // Called when he fires the shockwave
    public void Burst()
    {
        burstTimer = 0.5f;
        timer = 0f;
    }
 
    void Update()
    {
        if (lines == null)
            return;
 
        float dt = Time.deltaTime;
        if (burstTimer > 0f)
            burstTimer -= dt;
        bool bursting = burstTimer > 0f;
 
        // Re-roll the arcs every fraction of a second so they crackle
        timer -= dt;
        if (timer <= 0f)
        {
            for (int i = 0; i < arcCount; i++)
                Rebuild(i, bursting);
            timer = bursting ? 0.02f : UnityEngine.Random.Range(0.05f, 0.12f);
        }
 
        // Every frame: keep the arcs attached to him
        Vector3 center = transform.position;
        for (int i = 0; i < arcCount; i++)
        {
            if (!lines[i].enabled)
                continue;
            for (int p = 0; p < Points; p++)
                lines[i].SetPosition(p, new Vector3(center.x + offsets[i][p].x, center.y + offsets[i][p].y, center.z));
        }
    }
 
    void Rebuild(int i, bool bursting)
    {
        LineRenderer lr = lines[i];
 
        // Normally only some arcs are lit at any moment; during a burst they all are
        if (!bursting && UnityEngine.Random.value > 0.7f)
        {
            lr.enabled = false;
            return;
        }
        lr.enabled = true;
 
        float w = width * (bursting ? 1.6f : 1f) * UnityEngine.Random.Range(0.7f, 1.3f);
        lr.startWidth = w;
        lr.endWidth = w * 0.6f;
        Color col = UnityEngine.Random.value < 0.3f ? (Color)new Color32(190, 255, 255, 255) : (Color)new Color32(0, 210, 255, 255);
        lr.startColor = col;
        lr.endColor = col;
 
        float r = radius * (bursting ? burstScale : 1f);
        float a0 = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
 
        if (UnityEngine.Random.value < 0.3f)
        {
            // A bolt shooting outward from near his centre
            Vector2 dir = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0));
            Vector2 perp = new Vector2(-dir.y, dir.x);
            Vector2 s = dir * (0.3f * r);
            Vector2 e = dir * (1.4f * r);
            for (int p = 0; p < Points; p++)
            {
                float t = p / (Points - 1f);
                float jag = (p == 0 || p == Points - 1) ? 0f : UnityEngine.Random.Range(-0.25f, 0.25f) * r;
                offsets[i][p] = Vector2.Lerp(s, e, t) + perp * jag;
            }
        }
        else
        {
            // A jagged arc running part of the way around him
            float span = UnityEngine.Random.Range(40f, 110f) * Mathf.Deg2Rad * (UnityEngine.Random.value < 0.5f ? 1f : -1f);
            float r0 = r * UnityEngine.Random.Range(0.7f, 1.1f);
            for (int p = 0; p < Points; p++)
            {
                float t = p / (Points - 1f);
                float ang = a0 + span * t;
                float jag = (p == 0 || p == Points - 1) ? 0f : UnityEngine.Random.Range(-0.25f, 0.25f);
                offsets[i][p] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (r0 * (1f + jag));
            }
        }
    }
 
    void OnDestroy()
    {
        if (lines != null)
        {
            foreach (LineRenderer lr in lines)
                if (lr != null)
                    Destroy(lr.gameObject);
        }
        if (mat != null)
            Destroy(mat);
    }
}
public class Blue : MonoBehaviour
{
    AngryBird bird;
    void Start()
    {
        bird = GetComponent<AngryBird>();
        if (!gameObject.GetComponent<TriggerEventBehaviour>())
        {
            gameObject.AddComponent<UseEventTrigger>().Action = () =>
            {
                bird.GetComponent<PhysicalBehaviour>().HasOutline = false;
                bird.mouth.clip = bird.use[UnityEngine.Random.Range(1, 4)];
                bird.mouth.Play();
                ModAPI.CreateParticleEffect("Vapor", transform.position);
                GameObject c1, c2;
                c1 = Instantiate(gameObject, transform.position + transform.up * 0.3f, transform.rotation);
                c2 = Instantiate(gameObject, transform.position + transform.up * -0.3f, transform.rotation);
                c1.GetComponent<Rigidbody2D>().velocity = bird.rb.velocity + new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f));
                c2.GetComponent<Rigidbody2D>().velocity = bird.rb.velocity + new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f));
            };
        }
    }
}
public class Terence : MonoBehaviour
{
    AngryBird bird;
    public ExplosiveBehaviour explosionSource;
    [SkipSerialisation]
    public bool firTimeToStep = true;
    float _timer = 1.5f;
    void Start()
    {
        bird = GetComponent<AngryBird>();
        bird.wireAction = AngryBird.WireAction.DestroyOnHighSpeed;
        // No explosions at all: remove the explosive component so it can't go off on its own either
        if (explosionSource != null)
            Destroy(explosionSource);
    }
    void Update()
    {
        if (_timer < 0)
            return;
        if (_timer > 0 && firTimeToStep == false)
        {
            CameraShakeBehaviour.main.Shake(256f, transform.position);
            _timer -= Time.deltaTime;
        }
    }
    void OnCollisionEnter2D(Collision2D c)
    {
        if (firTimeToStep && bird.mouth != null)
            bird.mouth.PlayOneShot(ModAPI2.LoadSound("AudioClip/Terence_ability1.wav"));
        firTimeToStep = false;
    }
}
public class Tony : MonoBehaviour
{
    AngryBird bird;
    Rigidbody2D rb;
    void Start()
    {
        bird = GetComponent<AngryBird>();
        rb = bird.GetComponent<Rigidbody2D>();
        if (!gameObject.GetComponent<TriggerEventBehaviour>())
        {
            gameObject.AddComponent<UseEventTrigger>().Action = () =>
            {
                StartCoroutine("Activate");
            };
        }
    }
    IEnumerator Activate()
    {
        bird.stat = AngryBird.Status.Idle;
        rb.rotation = 0;
        rb.drag = 80;
        rb.freezeRotation = true;
        GetComponent<AudioSource>().PlayOneShot(ModAPI2.LoadSound("AudioClip/Brake.wav"));
        yield return new WaitForSeconds(1);
        rb.freezeRotation = false;
        rb.drag = 0;
        rb.velocity = Vector2.up * 16;
        yield return new WaitForSeconds(0.2f);
        rb.velocity = Vector2.up * -80f;
    }
}
public class Matilda : MonoBehaviour
{
    AngryBird bird;
    bool crumpled = false;
    Sprite crumpledSprite;
    void Start()
    {
        bird = GetComponent<AngryBird>();
        if (!gameObject.GetComponent<TriggerEventBehaviour>())
        {
            gameObject.AddComponent<UseEventTrigger>().Action = () =>
            {
                crumpled = true;
                crumpledSprite = ModAPI2.LoadSprite("MatildaCrumpled.png", 1f);
                GetComponent<SpriteRenderer>().sprite = crumpledSprite;

                bird.rb.velocity = new Vector2(bird.rb.velocity.x, 16);
                bird.mouth.clip = bird.use[UnityEngine.Random.Range(1, 4)];
                bird.mouth.Play();
                GameObject e = Instantiate(ModAPI.FindSpawnable("Brick").Prefab);
                e.transform.position = transform.position + new Vector3(0, -1f, 0);
                e.GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("Egg.png", 1.5f);
                e.gameObject.AddComponent<ExplosiveBehaviour>();
                e.gameObject.GetComponent<ExplosiveBehaviour>().Range = 18f;
                e.gameObject.GetComponent<ExplosiveBehaviour>().FragmentForce = 34f;
                e.gameObject.GetComponent<ExplosiveBehaviour>().ShotExplodeChance = -1f;
                e.gameObject.GetComponent<ExplosiveBehaviour>().BurnPower = 0f;
                e.gameObject.GetComponent<ExplosiveBehaviour>().ExplodesOnFragmentHit = false;
                e.gameObject.GetComponent<ExplosiveBehaviour>().ImpactForceThreshold = 0.1f;
                e.gameObject.GetComponent<ExplosiveBehaviour>().BigExplosion = true;
                e.gameObject.GetComponent<ExplosiveBehaviour>().DestroyOnExplode = true;
                e.gameObject.GetComponent<ExplosiveBehaviour>().Delay = 0f;
                e.gameObject.GetComponent<ExplosiveBehaviour>().ArmOnUse = false;
                e.GetComponent<Rigidbody2D>().velocity = 6 * -Vector2.up;
            };
        }
    }
    // The bird script swaps the sprite back after stuns (Idle/RecoverSprite),
    // so this keeps the crumpled one on.
    void LateUpdate()
    {
        if (crumpled && crumpledSprite != null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr.sprite != crumpledSprite)
                sr.sprite = crumpledSprite;
        }
    }
}
public class Hal : MonoBehaviour
{
    AngryBird bird;
    public bool onAction = false;
    float maxVelocity = 40, maxTorque = 50, mass;
    void Start()
    {
        bird = GetComponent<AngryBird>();
        gameObject.AddComponent<UseEventTrigger>().Action = () =>
        {
            bird.wireAction = AngryBird.WireAction.DestroyOnHighSpeed;
            mass = bird.rb.mass;
            onAction = true;
            bird.enableSelfBalancing = false;
            bird.Say(bird.use);
            bird.rb.mass = bird.gameMass * 3;
        };
    }
    void Update()
    {
        if(onAction)
        {
            bird.rb.drag = 2;
            if (transform.localScale.x > 0)
            {
                bird.rb.AddForce(Vector2.right * Time.deltaTime * -200 * bird.rb.mass, ForceMode2D.Impulse);
                bird.rb.AddTorque(Time.deltaTime * -50 * bird.rb.mass, ForceMode2D.Impulse);
            }
            if (transform.localScale.x < 0)
            {
                bird.rb.AddForce(Vector2.right * Time.deltaTime * 200 * bird.rb.mass, ForceMode2D.Impulse);
                bird.rb.AddTorque(Time.deltaTime * 50 * bird.rb.mass, ForceMode2D.Impulse);
            }
        }
    }
    void OnCollisionEnter2D(Collision2D c)
    {
        onAction = false;
        bird.enableSelfBalancing = true;
        Invoke("Reset", 1);
    }
    void Reset()
    {
        bird.rb.mass = bird.mass;
    }
}
public class Bubbles : MonoBehaviour
{
    AngryBird bird;
    int phase = 0;
    float size = 1;
    void Start()
    {
        bird = GetComponent<AngryBird>();
        gameObject.AddComponent<UseEventTrigger>().Action = () =>
        {
            GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("Bub_ability.png", 10.2f);
            size = transform.localScale.x;
            bird.isTerence = true;
            phase = 1;
            bird.rb.mass = 25;
            bird.wireAction = AngryBird.WireAction.Destroy;
            bird.Say(bird.use);
        };
    }
    void Update()
    {
        switch (phase)
        {
            case 1:
                transform.localScale = Vector2.one * size;
                size += Time.deltaTime * 60;
                if (size >= 10.2f)
                {
                    phase = 2;
                    size = 0;
                }
                break;
            case 2:
                size += Time.deltaTime;
                if (size > 3)
                    phase = 3;
                break;
            case 3:
                ModAPI.CreateParticleEffect("BigExplosion", transform.position);   // the same explosion, still with no force
                phase = 0;
                Destroy(gameObject);                                              // and he is gone
                break;
        }
    }
    void OnCollisionStay2D(Collision2D c)
    {
        if (phase == 1 && c.gameObject.GetComponent<Rigidbody2D>())
        {
            Utility.AddReasonableForceImpulse(c.gameObject.GetComponent<Rigidbody2D>(), c.transform.position - transform.position, 4, 25);
        }
    }
}
public class Stella : MonoBehaviour
{
    AngryBird bird;
    GameObject[] targets;
    float timer = -1;

    void Start()
    {
        bird = GetComponent<AngryBird>();
        gameObject.AddComponent<UseEventTrigger>().Action = () =>
        {
            bird.Say(bird.use);
            Activate();
        };
    }
    void Update()
    {
        if(timer > 0)
            timer -= Time.deltaTime;
    }
    void Activate()
    {
        timer = 0.1f;
        CircleCollider2D triggerCollider;
        triggerCollider = GetComponent<CircleCollider2D>();
        triggerCollider.enabled = false;
        triggerCollider.enabled = true;
    }
    void OnTriggerEnter2D(Collider2D c)
    {
        if (timer < 0)
            return;
        if (Utility.Distance(c.transform.position, transform.position) > 4)
            return;
        if (c.gameObject.layer == 11)
            return;
        if (c.gameObject.GetComponentInChildren<StellaBubble>() != null)
            return;
        GameObject bubble = new GameObject();
        bubble.AddComponent<StellaBubble>();
        bubble.GetComponent<StellaBubble>().target = c.gameObject;
        bubble.transform.position = c.transform.position;
        bubble.transform.parent = c.transform;
    }
}
public class StellaBubble : MonoBehaviour
{
    public GameObject target;
    float timer, scaleMeter = 0, lifeTime = 0;
    AudioSource bubbleSound;
    Rigidbody2D rb;
    void Start()
    {
        gameObject.AddComponent<SpriteRenderer>().sortingOrder = target.GetComponent<SpriteRenderer>().sortingOrder + 1;
        GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("PinkBird_Bubble.png");
        timer = UnityEngine.Random.Range(4.0f, 8.0f);
        bubbleSound = gameObject.AddComponent<AudioSource>();
        rb = target.GetComponent<Rigidbody2D>();
        if (target.gameObject.GetComponent<SteelBindingBehaviour>() != null)
            Destroy(target.gameObject.GetComponent<SteelBindingBehaviour>());
        if (target.gameObject.GetComponent<WoodenBindingBehaviour>() != null)
            Destroy(target.gameObject.GetComponent<WoodenBindingBehaviour>());
        if (target.gameObject.GetComponent<RopeBehaviour>() != null)
            Destroy(target.gameObject.GetComponent<RopeBehaviour>());
        if (target.gameObject.GetComponent<WoodStrutBehaviour>() != null)
            Destroy(target.gameObject.GetComponent<WoodStrutBehaviour>());
        scaleMeter = 0;
    }
    void Update()
    {
        lifeTime += Time.deltaTime;
        timer -= Time.deltaTime;
        transform.localScale = scaleMeter * Vector2.one;
        TimerAnimation(timer);
        if (timer < 0)
            Pop();
        if(rb.mass < 25)
            rb.AddForce(rb.mass * Vector2.up * 2f * Time.deltaTime + rb.mass * 9.81f * Time.deltaTime * Vector2.up, ForceMode2D.Impulse);
        if (rb.mass >= 25)
            rb.AddForce(Vector2.up * 50f * Time.deltaTime + rb.mass * 9.81f * Time.deltaTime * Vector2.up, ForceMode2D.Impulse);
    }
    void TimerAnimation(float n)
    {
        if(lifeTime < 0.2f)
        {
            scaleMeter = lifeTime * 5;
        }
    }
    void Pop()
    {
        bubbleSound.volume = 0.2f;
        bubbleSound.PlayOneShot(ModAPI2.LoadSound("AudioClip/stella_bubble_pop_0" + UnityEngine.Random.Range(1, 5) + ".wav"));
        Destroy(gameObject, 2);
        Destroy(GetComponent<SpriteRenderer>());
        Destroy(this);
    }
}
public class Icebird : MonoBehaviour
{
    PhysicalBehaviour phy;
    AngryBird bird;
    SpriteRenderer sr;
 
    // Fuse: after a Slingshot X shot hits something, he slowly turns icy blue, then uses his ability and disappears
    public float fuseTime = 2.5f;                                     // seconds from the hit to the ability
    public Color32 fuseColor = new Color32(120, 200, 255, 255);    // the colour he fades toward
    public bool vanishAfterAbility = true;                          // he disappears the moment his ability goes off
    public bool freezeOnTouch = true;                               // any block he touches turns to glass (not only when he uses his ability)
    public float burstRadius = 4f;                                  // how far his ability's freezing burst reaches
 
    bool fusing = false;
    bool fuseFired = false;
    bool used = false;
    float fuseTimer = 0f;
    Color baseColor = Color.white;
 
    // What each wooden / stone / iron block turns into
    static string[] woodNames = { "Wooden Block", "Wooden Stick", "Wooden Box", "Wooden Triangle", "Wooden Circle" };
    static string[] stoneNames = { "Stone Block", "Stone Stick", "Stone Box", "Stone Triangle", "Stone Circle" };
    static string[] ironNames = { "Iron Block", "Iron Stick", "Iron Box", "Iron Triangle", "Iron Circle" };
    static string[] glassNames = { "Glass Block", "Glass Pole", "Glass Box", "Glass Triangle", "Glass Circle" };
    static string[] glassSprites = { "glasspole.png", "glassstick.png", "glassbox.png", "glassilluminati.png", "glassrock.png" };
    static float[] glassHp = { 7f, 7f, 7f, 7f, 14f };
    static float[] glassMass = { 15f, 15f, 15f, 15f, 30f };
 
    void Start()
    {
        bird = GetComponent<AngryBird>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            baseColor = sr.color;
        gameObject.AddComponent<UseEventTrigger>().Action = () =>
        {
            UseAbility();
        };
        phy = GetComponent<PhysicalBehaviour>();
    }
 
    void UseAbility()
    {
        if (used)
            return;
		BirdTrail iceTrail = GetComponent<BirdTrail>();
        if (iceTrail != null)
            iceTrail.MarkAbility();
        bird.Say(bird.use);
        Activate();
    }
 
    void Activate()
    {
        GetComponent<AudioSource>().PlayOneShot(ModAPI2.LoadSound("AudioClip/icebird_voice_ability.wav"));
        GameObject particle = ModAPI.CreateParticleEffect("BigExplosion", transform.position + Vector3.up * 0.5f);
        Destroy(particle.GetComponent<AudioSource>());
        var color = particle.GetComponent<ParticleSystem>().main;
        color.startColor = (Color)new Color32(240, 240, 240, 200);
        var color0 = particle.GetComponentsInChildren<ParticleSystem>()[0].main;
        color0.gravityModifier = UnityEngine.Random.Range(0.1f, 16.0f);
        color0.startColor = (Color)new Color32(0, 200, 240, 150);
        var color1 = particle.GetComponentsInChildren<ParticleSystem>()[4].main;
        color1.startColor = (Color)new Color32(0, 150, 200, 200);
 
        // The freezing burst happens right now
        FreezeBurst();
 
        // ...and he is gone straight away (the cloud and his voice carry on without him)
        if (vanishAfterAbility)
        {
            used = true;
            bird.Vanish(3f);
        }
    }
 
    // Freezes everything close by
    void FreezeBurst()
    {
        HashSet<GameObject> done = new HashSet<GameObject>();
        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, burstRadius + 1f);
        foreach (Collider2D c in cols)
        {
            if (c == null)
                continue;
            GameObject go = c.gameObject;
            if (go == gameObject || !done.Add(go))
                continue;
            if (Utility.Distance(c.transform.position, transform.position) > burstRadius)
                continue;
            if (go.layer == 11)
                continue;
            if (go.GetComponentInChildren<StellaBubble>() != null)
                continue;
 
            PhysicalBehaviour cphy = go.GetComponent<PhysicalBehaviour>();
            if (cphy != null)
                cphy.Temperature = -1000;
            if (go.GetComponent<DamagableMachineryBehaviour>())
                go.GetComponent<DamagableMachineryBehaviour>().Break(Vector2.zero);
            FreezeBlock(go);
        }
    }
 
    void Update()
    {
        phy.Temperature = -30;
 
        if (used || bird == null || sr == null)
            return;
 
        bool shouldFuse = bird.shotFromSlingshot && bird.hasCollidedAfterShot;
        if (shouldFuse && !fuseFired)
        {
            if (!fusing)
            {
                fusing = true;
                fuseTimer = 0f;
                baseColor = sr.color;
            }
            fuseTimer += Time.deltaTime;
            sr.color = Color.Lerp(baseColor, fuseColor, Mathf.Clamp01(fuseTimer / fuseTime));
            if (fuseTimer >= fuseTime)
            {
                fuseFired = true;
                fusing = false;
                sr.color = baseColor;
                UseAbility();
            }
        }
        else if (!shouldFuse)
        {
            // caught by the slingshot again (or a fresh shot): reset the fuse
            if (fusing)
                sr.color = baseColor;
            fusing = false;
            fuseFired = false;
        }
    }
 
    // Turns one wooden / stone / iron block into glass (glass is left alone)
    void FreezeBlock(GameObject go)
    {
        Block blk = go.GetComponent<Block>();
        if (blk == null)
            return;
        if (blk.material == Block.Material.Glass)
            return;
 
        for (int i = 0; i < glassNames.Length; i++)
        {
            if (go.name == woodNames[i] || go.name == stoneNames[i] || go.name == ironNames[i])
            {
                go.name = glassNames[i];
                blk.hp = glassHp[i];
                blk.fullHp = glassHp[i];
                Rigidbody2D brb = go.GetComponent<Rigidbody2D>();
                if (brb != null)
                    brb.mass = glassMass[i];
                go.GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite(glassSprites[i]);
                go.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("ice");
                blk.material = Block.Material.Glass;
                return;
            }
        }
    }
 
    // Touching a block (flying, rolling, resting on it...) turns it to glass
    void OnCollisionEnter2D(Collision2D c)
    {
        if (freezeOnTouch)
            FreezeBlock(c.gameObject);
    }
 
    void OnCollisionStay2D(Collision2D c)
    {
        if (freezeOnTouch)
            FreezeBlock(c.gameObject);
    }
}
public class Silver : MonoBehaviour
{
    AngryBird bird;
    bool active = false;
    bool hitSomething = false;
    float activeTime = 0f;
 
    // Tuning knobs
    public float loopSpeed = 40f;           // how fast she flies around the loop
    public float loopRadius = 2f;         // how big the loop is (small, but big enough to see). Bigger = a wider, longer loop
    public float diveSpeed = 40f;           // how fast she dives straight down
    public float diveMassMultiplier = 1.4f;   // heavier on the dive = hits harder
    public float maxDiveTime = 4f;          // safety limit, in case she never hits anything
    public float collisionGrace = 0.2f;    // seconds after activation in which touching something doesn't count
    public bool facesRight = true;          // set to false if her sprite faces LEFT and she turns the wrong way
    public Color32 featherColor = new Color32(210, 210, 225, 255);
 
    void Start()
    {
        bird = GetComponent<AngryBird>();
 
        // Same guard the other birds use, so the ability isn't hooked up twice
        if (!gameObject.GetComponent<TriggerEventBehaviour>())
        {
            gameObject.AddComponent<UseEventTrigger>().Action = () =>
            {
                if (active)
                    return;
                BirdTrail trail = GetComponent<BirdTrail>();
                if (trail != null)
                    trail.MarkAbility();
                StartCoroutine(LoopAndDive());
            };
        }
    }
 
    // The z angle that makes her face a direction (same idea as Mighty Feathers Red)
    float AngleOf(Vector2 dir)
    {
        float a = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        bool flipped = transform.localScale.x < 0f;
        if (facesRight == flipped)
            a += 180f;
        return a;
    }
 
    void OnCollisionEnter2D(Collision2D c)
    {
        if (active && activeTime >= collisionGrace)
            hitSomething = true;
    }
 
    // Says a random one of her ability voicelines (only the ones that exist)
    void SayAbility()
    {
        if (bird == null || bird.mouth == null || bird.use == null)
            return;
        List<AudioClip> clips = new List<AudioClip>();
        for (int i = 1; i < bird.use.Length && i <= 3; i++)
            if (bird.use[i] != null)
                clips.Add(bird.use[i]);
        if (clips.Count == 0)
            return;
        bird.mouth.clip = clips[UnityEngine.Random.Range(0, clips.Count)];
        bird.mouth.Play();
    }
 
    IEnumerator LoopAndDive()
    {
        active = true;
        hitSomething = false;
        activeTime = 0f;
        bird.freeRotation = true;             // so the bird's own "point along the flight" rule doesn't fight the loop
        bird.enableSelfBalancing = false;
        float originalGravity = bird.rb.gravityScale;
        float originalMass = bird.rb.mass;
 
        try
        {
            bird.rb.gravityScale = 0f;        // no gravity: the loop stays round, the dive stays straight
 
            // Which way is she going? (if she's standing still, the way she faces)
            Vector2 dir = bird.rb.velocity;
            if (dir.sqrMagnitude < 1f)
            {
                bool faceRight = (transform.localScale.x >= 0f) == facesRight;
                dir = faceRight ? Vector2.right : Vector2.left;
            }
            dir.Normalize();
 
            SayAbility();
            Utility.CreateFeatherEffect(featherColor, transform.position);
 
            // ---- Phase 1: the loop ----
            // Up and over: going right turns anticlockwise, going left turns clockwise.
            // She turns a whole 360 degrees in whole physics steps, so the loop closes on itself.
            float turn = dir.x >= 0f ? 1f : -1f;
            float loopTime = 2f * Mathf.PI * loopRadius / Mathf.Max(0.1f, loopSpeed);
            int steps = Mathf.Max(8, Mathf.RoundToInt(loopTime / Time.fixedDeltaTime));
            float stepAngle = 360f / steps * turn;
 
            for (int i = 0; i < steps && !hitSomething; i++)
            {
                bird.rb.velocity = dir * loopSpeed;
                bird.rb.angularVelocity = 0f;
                bird.rb.rotation = AngleOf(dir);
                dir = (Vector2)(Quaternion.Euler(0f, 0f, stepAngle) * dir);
                activeTime += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
 
            // ---- Phase 2: the dive, straight down, until she hits something ----
            if (!hitSomething)
            {
                bird.wireAction = AngryBird.WireAction.DestroyOnHighSpeed;
                bird.rb.mass = originalMass * diveMassMultiplier;
                ModAPI.CreateParticleEffect("Flash", transform.position);
				bird.rb.gravityScale = 1f;        
 
                float flown = 0f;
                while (!hitSomething && flown < maxDiveTime)
                {
                    bird.rb.velocity = Vector2.down * diveSpeed;
                    bird.rb.angularVelocity = 0f;
                    bird.rb.rotation = AngleOf(Vector2.down);
                    flown += Time.fixedDeltaTime;
                    activeTime += Time.fixedDeltaTime;
                    yield return new WaitForFixedUpdate();
                }
            }
        }
        finally
        {
            // ---- Phase 3: back to normal (always, even if something interrupts the ability) ----
            if (bird != null && bird.rb != null)
            {
                bird.rb.mass = originalMass;
                bird.rb.gravityScale = originalGravity;
            }
            if (bird != null)
                bird.freeRotation = false;
            active = false;
        }
    }
}
public class SpaceEgg : MonoBehaviour
{
    AngryBird bird;
    bool busy = false;
 
    // Tuning knobs
    public float pullRadius = 14f;       // how far the wormhole reaches
    public float pullTime = 0.4f;        // "a split second": how long the pull lasts
    public float pullStrength = 44f;     // how hard it pulls (speed gained per second)
    public float nearBoost = 0.4f;       // 0 = same pull everywhere, higher = much stronger pull close to the middle
    public float gatherRadius = 0f;    // things closer than this to the middle get slowed so they clump together
    public float gatherDamping = 0f;  // how much they're slowed each physics step (0 = no clumping)
    public float vortexRadius = 4f;      // size of the wormhole picture
    public float vortexSpin = 440f;      // how fast the wormhole spins (degrees per second)
    public float vortexTurns = 4f;       // how tightly wound the spiral is
    public Color32 vortexColor = new Color32(120, 90, 255, 255);
 
    void Start()
    {
        bird = GetComponent<AngryBird>();
 
        if (!gameObject.GetComponent<TriggerEventBehaviour>())
        {
            gameObject.AddComponent<UseEventTrigger>().Action = () =>
            {
                if (busy)
                    return;
                StartCoroutine(Collapse());
            };
        }
    }
 
    IEnumerator Collapse()
    {
        busy = true;
 
        Vector2 center = (Vector2)transform.position;   // the wormhole stays where the egg was
        Vector3 originalScale = transform.localScale;
        Utility.DestroyWires(gameObject);               // so nothing yanks the egg around while it collapses
        bird.rb.simulated = false;                      // frozen out of physics (also keeps it from pulling itself)
 
        // The wormhole opens (no voice on purpose)
        TeleVortex v = TeleVortex.Create(center, vortexRadius, pullTime + 0.3f, vortexColor);
        v.spin = vortexSpin;
        v.turns = vortexTurns;
        ModAPI.CreateParticleEffect("Flash", center);
        Utility.CreateOneShotAudio(ModAPI2.LoadSound("AudioClip/warp.wav"), center, 2f);   // plays for up to 4 seconds (raise the 4f if warp.wav is longer)
 
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        bool hidden = false;
 
        float t = 0f;
        while (t < pullTime)
        {
            // The egg shrinks and spins into the wormhole during the first 0.2s, then it's gone
            if (t < 0.2f)
            {
                transform.localScale = originalScale * Mathf.Max(0.01f, 1f - t / 0.2f);
                transform.Rotate(0f, 0f, 720f * Time.fixedDeltaTime);
            }
            else if (!hidden)
            {
                hidden = true;
                for (int i = 0; i < renderers.Length; i++)
                    if (renderers[i] != null)
                        renderers[i].enabled = false;
            }
 
            Pull(center);
            t += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
 
        // Done: the egg disappears for good (the wormhole picture shrinks away by itself)
        Destroy(gameObject);
    }
 
    void Pull(Vector2 center)
    {
        Collider2D[] cols = Physics2D.OverlapCircleAll(center, pullRadius);
        HashSet<Rigidbody2D> done = new HashSet<Rigidbody2D>();
 
        foreach (Collider2D c in cols)
        {
            if (c == null)
                continue;
            Rigidbody2D rb = c.attachedRigidbody;
            if (rb == null || rb == bird.rb || rb.bodyType != RigidbodyType2D.Dynamic)
                continue;
            if (!done.Add(rb))                  // each body only once, however many colliders it has
                continue;
 
            Vector2 to = center - rb.position;
            float dist = to.magnitude;
            if (dist < 0.05f)
                continue;
            Vector2 dir = to / dist;
 
            // stronger the closer it is to the middle
            float closeness = 1f - Mathf.Clamp01(dist / pullRadius);
            float strength = pullStrength * (1f + nearBoost * closeness * 4f);
 
            rb.velocity = rb.velocity + dir * strength * Time.fixedDeltaTime;
 
            // things that reach the middle get slowed so they pile up instead of shooting out the other side
            if (dist < gatherRadius)
                rb.velocity = rb.velocity * (1f - gatherDamping);
        }
    }
}
public class CanBird : MonoBehaviour
{
    AngryBird bird;
    SpriteRenderer sr;
    bool wrapped = false;
 
    void Start()
    {
        bird = GetComponent<AngryBird>();
        sr = GetComponent<SpriteRenderer>();
    }
 
    void Update()
    {
        if (bird == null)
            return;
 
        // Once the can is activated, it must not "go idle and vanish" before the eagle arrives
        if (!wrapped)
        {
            UseEventTrigger[] triggers = GetComponents<UseEventTrigger>();
            if (triggers.Length > 0)
            {
                wrapped = true;
                for (int i = 0; i < triggers.Length; i++)
                {
                    if (triggers[i].Action == null)
                        continue;
                    var original = triggers[i].Action;
                    triggers[i].Action = () =>
                    {
                        bird.idleDespawnTime = float.MaxValue;
                        original();
                    };
                }
            }
        }
    }
 
    void LateUpdate()
    {
        if (bird == null || sr == null)
            return;
        bird.enableSelfBalancing = false;                 // a can doesn't stand itself upright like a bird
        if (bird.sprite[0] != null && sr.sprite != bird.sprite[0])
            sr.sprite = bird.sprite[0];                   // never switch to the stunned picture
    }
}
public class MightyEagleCan : MonoBehaviour
{
    // Timing
    public float delay = 2f;          // "takes a couple seconds" after you activate it
    public float screechLead = 1.5f;  // gap between the screech and the eagle appearing
    public float swoopTime = 0.7f;    // seconds the eagle takes to cross the screen
    public float cooldown = 1f;       // before the can can be used again
 
    // Eagle looks / reach
    public float eagleScale = 2f;     // size of the eagle sprite
    public float hitRadius = 8f;    // how wide a path of destruction he cuts (world units)
    public bool spriteFacesRight = true;   // set false if MightyEagle.png is drawn facing LEFT
 
    // Where he comes from: angle above the horizon, in degrees (0 = right, 90 = straight up, 180 = left)
    public float minAngle = 70f;
    public float maxAngle = 170f;
	
	public float nudgeExtra = 2f;        // how much WIDER than the destruction path the nudge zone is
    public float nudgeForce = 5f;        // speed given to nudged things (Shockwave uses 25; keep this much lower)
    public bool destroyCan = true;       // the eagle destroys the can as he passes it
    public bool playBreakSounds = true;  // glass / wood / stone break sounds for destroyed blocks
 
    bool ready = true;
 
    void Start()
    {
        if (!gameObject.GetComponent<TriggerEventBehaviour>())
        {
            gameObject.AddComponent<UseEventTrigger>().Action = () =>
            {
                if (!ready)
                    return;
                ready = false;
                StartCoroutine(Summon());
            };
        }
    }
 
    IEnumerator Summon()
    {
        yield return new WaitForSeconds(delay);
 
        // The screech, same sound Tony plays. (2D audio, so it's loud wherever the camera is.)
        Utility.CreateOneShotAudio(ModAPI2.LoadSound("AudioClip/Brake.wav"), transform.position, 4f);
        yield return new WaitForSeconds(screechLead);
 
        SpawnEagle();
 
        yield return new WaitForSeconds(cooldown);
        ready = true;
    }
 
    void SpawnEagle()
    {
        Vector2 canPos = transform.position;
        Camera cam = Camera.main;
 
        // Start and end far enough away to be off-screen wherever the can and camera are,
        // so you never see the eagle pop in or out.
        float reach = 20f;
        float camDist = 0f;
        if (cam != null)
        {
            reach = cam.orthographicSize * Mathf.Sqrt(cam.aspect * cam.aspect + 1f);   // centre-to-corner of the view
            camDist = Vector2.Distance(canPos, cam.transform.position);
        }
        float margin = reach + camDist + 3f * eagleScale;
 
        // Random direction in the sky -> through the can -> out the other side (and through the floor)
        float a = UnityEngine.Random.Range(minAngle, maxAngle) * Mathf.Deg2Rad;
        Vector2 fromCan = new Vector2(Mathf.Cos(a), Mathf.Sin(a));   // can -> sky
        Vector2 dir = -fromCan;                                       // travel direction
        Vector2 start = canPos + fromCan * margin;
        float totalDistance = margin * 2f;
 
        // Rotate toward the can. If he's flying leftwards, flip the sprite so he isn't upside-down.
        float theta = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float rotation = spriteFacesRight ? theta : theta + 180f;
        bool flip = spriteFacesRight ? dir.x < 0f : dir.x > 0f;
 
        GameObject eagle = new GameObject("MightyEagle");
        eagle.transform.position = start;
        eagle.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        eagle.transform.localScale = Vector3.one * eagleScale;
 
        SpriteRenderer sr = eagle.AddComponent<SpriteRenderer>();
        sr.sprite = ModAPI2.LoadSprite("MightyEagle.png", 1f);
        sr.sortingOrder = 50;        // in front of everything
        sr.flipY = flip;
 
        // No collider on purpose: he flies straight through the floor and everything else.
        EagleSwoop swoop = eagle.AddComponent<EagleSwoop>();
        swoop.direction = dir;
        swoop.speed = totalDistance / swoopTime;
        swoop.distance = totalDistance;
        swoop.hitRadius = hitRadius;
        swoop.ignore = gameObject;
		swoop.nudgeExtra = nudgeExtra;
        swoop.nudgeForce = nudgeForce;
        swoop.destroyCan = destroyCan;
        swoop.playBreakSounds = playBreakSounds;
    }
}
public class EagleSwoop : MonoBehaviour
{
    public Vector2 direction;
    public float speed;
    public float distance;
    public float hitRadius = 2.5f;
    public GameObject ignore;      // the can that summoned him
 
    // Set by MightyEagleCan
    public float nudgeExtra = 2f;
    public float nudgeForce = 5f;
    public bool destroyCan = true;
    public bool playBreakSounds = false;   // unused now; only here so MightyEagleCan still compiles
 
    float traveled = 0f;
    HashSet<GameObject> done = new HashSet<GameObject>();     // already processed by the destruction pass
    HashSet<GameObject> gone = new HashSet<GameObject>();     // deleted this frame (Destroy() only lands at the end of the frame)
    HashSet<Rigidbody2D> nudged = new HashSet<Rigidbody2D>(); // each thing gets nudged once
 
    void Update()
    {
        float step = speed * Time.deltaTime;
        if (step <= 0f)
            return;                // game paused
 
        Vector2 from = transform.position;
 
        // 1) Destruction zone: sweep a circle along this frame's movement
        foreach (RaycastHit2D hit in Physics2D.CircleCastAll(from, hitRadius, direction, step))
        {
            if (hit.collider != null)
                Obliterate(hit.collider);
        }
 
        // 2) Nudge zone: a wider sweep that gently shoves whatever is still standing
        foreach (RaycastHit2D hit in Physics2D.CircleCastAll(from, hitRadius + nudgeExtra, direction, step))
        {
            if (hit.collider != null)
                Nudge(hit.collider);
        }
 
        transform.position = from + direction * step;
        traveled += step;
        if (traveled >= distance)
            Destroy(gameObject);
    }
 
    void Obliterate(Collider2D c)
    {
        GameObject go = c.attachedRigidbody != null ? c.attachedRigidbody.gameObject : c.gameObject;
        if (go == null || !done.Add(go))
            return;
        if (go.GetComponent<GroundBlock>() != null)     // the ground can't be destroyed
            return;
 
        // The sardine can: the eagle takes it out as he passes
        if (go == ignore)
        {
            if (destroyCan)
            {
                Utility.DestroyWires(go);
                Utility.CreateAnimation("Pop", 8, go.transform.position, new Color32(190, 190, 190, 255));
                ModAPI.CreateParticleEffect("Flash", go.transform.position);
                gone.Add(go);
                Destroy(go);
            }
            return;
        }
        if (go.GetComponent<MightyEagleCan>())    // any other sardine cans lying around
            return;
 
        // Pigs and blocks get the same treatment as everything else, but the usual "skip" rules
        // (ground layer, "Player" tag) must not stop them. Pigs are tagged "Player" too.
        bool isPig = go.GetComponent<BadPig>() != null
                  || (go.GetComponent<ABcharacters>() != null && go.GetComponent<AngryBird>() == null);
        bool isBlock = go.GetComponent<Block>() != null;
 
        if (!isPig && !isBlock)
        {
            if (c.attachedRigidbody == null)      // no rigidbody = the ground / static scenery
                return;
            if (go.layer == 11)                   // same "ground" layer Bomb and Terence check
                return;
            if (go.CompareTag("Player"))          // birds, slingshot, egg, crown
                return;
            if (go.GetComponent<AngryBird>() != null)
                return;
            if (go.GetComponent<LimbBehaviour>() != null || go.GetComponent<DebrisComponent>() != null)   // people, debris
                return;
        }
 
        // Gone.
        Utility.DestroyWires(go);
        ModAPI.CreateParticleEffect("Flash", go.transform.position);
        gone.Add(go);
        Destroy(go);
    }
 
    // A light shove away from his flight line (plus a little in his direction of travel)
    void Nudge(Collider2D c)
    {
        Rigidbody2D target = c.attachedRigidbody;
        if (target == null || target.bodyType != RigidbodyType2D.Dynamic)
            return;
        GameObject go = target.gameObject;
        if (go == ignore || gone.Contains(go) || go.layer == 11 || !nudged.Add(target))
            return;
 
        Vector2 offset = target.position - (Vector2)transform.position;
        Vector2 sideways = offset - direction * Vector2.Dot(offset, direction);   // the part of the offset that points away from his path
        if (sideways.sqrMagnitude < 0.0001f)
            sideways = new Vector2(-direction.y, direction.x) * (UnityEngine.Random.value < 0.5f ? 1f : -1f);
 
        Vector2 push = (sideways.normalized + direction * 0.5f).normalized;       // mostly sideways, a little forward
        Utility.AddReasonableForceImpulse(target, push, nudgeForce, 100f);
    }
}
public class SpaceEagleCan : MonoBehaviour
{
    // Timing
    public float wormholeTime = 4f;       // seconds between activating the can and the eagle emerging
 
    // Wormhole looks
    public float wormholeRadius = 4f;
    public float wormholeSpin = 200f;     // degrees per second
    public float wormholeTurns = 2f;      // how tightly wound the spiral is
    public Color32 wormholeColor = new Color32(120, 90, 255, 255);
 
    // The eagle's flight (he faces the camera, grows as if flying toward it, and drifts off screen)
    public float eagleStartScale = 0.1f;  // size when he first emerges
    public float eagleEndScale = 4f;      // size when he leaves the screen
    public float flyTime = 2f;          // seconds from emerging to leaving the screen
    public float minAngle = 45f;          // direction he drifts off in: 0 = right, 90 = up, 180 = left
    public float maxAngle = 135f;
 
    // The blast when he emerges
    public float destroyRadius = 4f;      // blocks, pigs and loose objects this close are destroyed
    public float blastRadius = 14f;       // everything out to here is launched outward
    public float blastTime = 0.5f;        // how fast the blast spreads out to blastRadius
    public float blastSpeed = 44f;        // launch speed at the centre (fades toward the edge)
    public float massThreshold = 74f;     // bodies lighter than this get the full speed; heavier ones proportionally less
    public bool killPigsInBlast = true;   // pigs outside the destroy zone but inside the blast are knocked out
    public float shakeStrength = 248f;    // same number Terence / Shockwave use
    public float shakeTime = 1f;
    public bool playScreech = true;       // the same screech the Mighty Eagle uses, played as he emerges
    public bool destroyCan = true;        // the emerging eagle obliterates the can
 
    bool ready = true;
 
    void Start()
    {
        if (!gameObject.GetComponent<TriggerEventBehaviour>())
        {
            gameObject.AddComponent<UseEventTrigger>().Action = () =>
            {
                if (!ready)
                    return;
                ready = false;
                StartCoroutine(Summon());
            };
        }
    }
 
    IEnumerator Summon()
    {
        Vector2 center = (Vector2)transform.position;   // the wormhole opens where the can is
 
        // Hold the can still while the wormhole opens
        Utility.DestroyWires(gameObject);
        Rigidbody2D canBody = GetComponent<Rigidbody2D>();
        if (canBody != null)
        {
            canBody.velocity = Vector2.zero;
            canBody.angularVelocity = 0f;
            canBody.simulated = false;
        }
 
        // The wormhole stays open while the eagle comes out, then shrinks away
        TeleVortex v = TeleVortex.Create(center, wormholeRadius, wormholeTime + 1f, wormholeColor);
        v.spin = wormholeSpin;
        v.turns = wormholeTurns;
 
        yield return new WaitForSeconds(wormholeTime);
 
        // ---- The eagle emerges ----
        SpawnEagle(center);
 
        // Effects at the moment of emerging
        Utility.CreateAnimation("Explosion", 15, center, new Color32(150, 120, 255, 255)).transform.localScale = Vector2.one * 6f;
        ModAPI.CreateParticleEffect("Flash", center).transform.localScale = Vector3.one * 4f;
        if (playScreech)
            Utility.CreateOneShotAudio(ModAPI2.LoadSound("AudioClip/Brake.wav"), center, 4f);
 
        // ...and he obliterates the can
        if (destroyCan)
        {
            Utility.CreateAnimation("Pop", 8, center, new Color32(190, 190, 190, 255));
            Destroy(gameObject);
        }
        else
        {
            if (canBody != null)
                canBody.simulated = true;
            ready = true;
        }
    }
 
    void SpawnEagle(Vector2 center)
    {
        Camera cam = Camera.main;
 
        // Fly far enough that he is off-screen wherever the can and camera are
        float reach = 20f;
        float camDist = 0f;
        if (cam != null)
        {
            reach = cam.orthographicSize * Mathf.Sqrt(cam.aspect * cam.aspect + 1f);   // centre-to-corner of the view
            camDist = Vector2.Distance(center, cam.transform.position);
        }
        float margin = reach + camDist + 3f * eagleEndScale;
 
        float a = UnityEngine.Random.Range(minAngle, maxAngle) * Mathf.Deg2Rad;
 
        GameObject eagle = new GameObject("SpaceEagle");
        eagle.transform.position = new Vector3(center.x, center.y, 0f);
        eagle.transform.localScale = Vector3.one * eagleStartScale;
 
        SpriteRenderer sr = eagle.AddComponent<SpriteRenderer>();
        sr.sprite = ModAPI2.LoadSprite("SpaceEagle.png", 1f);   // front view, facing the camera
        sr.sortingOrder = 50;                                    // in front of everything
 
        SpaceEagleFlight f = eagle.AddComponent<SpaceEagleFlight>();
        f.center = center;
        f.direction = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        f.margin = margin;
        f.flyTime = flyTime;
        f.startScale = eagleStartScale;
        f.endScale = eagleEndScale;
        f.destroyRadius = destroyRadius;
        f.blastRadius = blastRadius;
        f.blastTime = blastTime;
        f.blastSpeed = blastSpeed;
        f.massThreshold = massThreshold;
        f.killPigsInBlast = killPigsInBlast;
        f.shakeStrength = shakeStrength;
        f.shakeTime = shakeTime;
    }
}
 // The eagle himself: grows and drifts off screen while a blast spreads out from the wormhole.
public class SpaceEagleFlight : MonoBehaviour
{
    public Vector2 center;
    public Vector2 direction;
    public float margin = 40f;
    public float flyTime = 1.8f;
    public float startScale = 0.6f;
    public float endScale = 6f;
    public float destroyRadius = 7f;
    public float blastRadius = 16f;
    public float blastTime = 0.5f;
    public float blastSpeed = 45f;
    public float massThreshold = 84f;
    public bool killPigsInBlast = true;
    public float shakeStrength = 248f;
    public float shakeTime = 0.9f;
 
    float age = 0f;
    HashSet<GameObject> done = new HashSet<GameObject>();   // each thing is handled once
 
    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;                  // game paused
        age += dt;
 
        // Fly: starts slow, speeds up as he heads toward the camera and off the screen
        float t = Mathf.Clamp01(age / Mathf.Max(0.01f, flyTime));
        float ease = t * t;
        transform.position = new Vector3(center.x + direction.x * margin * ease, center.y + direction.y * margin * ease, transform.position.z);
        transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, t);
 
        // The blast: a ring that spreads out from the wormhole
        float ring = blastRadius * Mathf.Clamp01(age / Mathf.Max(0.01f, blastTime));
        if (ring > 0.05f)
            Blast(ring);
 
        if (age < shakeTime && CameraShakeBehaviour.main != null)
            CameraShakeBehaviour.main.Shake(shakeStrength, center);
 
        if (age >= flyTime)
            Destroy(gameObject);
    }
 
    void Blast(float ring)
    {
        foreach (Collider2D c in Physics2D.OverlapCircleAll(center, ring))
        {
            if (c == null)
                continue;
            float dist = Vector2.Distance(c.bounds.ClosestPoint(center), center);
            Process(c, dist);
        }
    }
 
    void Process(Collider2D c, float dist)
    {
        GameObject go = c.attachedRigidbody != null ? c.attachedRigidbody.gameObject : c.gameObject;
        if (go == null || go == gameObject || !done.Add(go))
            return;
 
        // Inside the destroy zone: obliterated
        if (dist <= destroyRadius && IsDestroyable(go, c))
        {
            Utility.DestroyWires(go);
            ModAPI.CreateParticleEffect("Flash", go.transform.position);
            Destroy(go);
            return;
        }
 
        // Everything else (birds, ragdolls, anything outside the destroy zone) is launched outward
        Rigidbody2D rb = c.attachedRigidbody;
        if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic || go.layer == 11)
            return;
 
        Vector2 offset = rb.position - center;
        Vector2 dir = offset.sqrMagnitude > 0.0001f ? offset.normalized : Vector2.up;
        float falloff = Mathf.Lerp(1f, 0.4f, Mathf.Clamp01(dist / blastRadius));
 
        Utility.DestroyWires(go);
        Utility.AddReasonableForceImpulse(rb, dir, blastSpeed * falloff, massThreshold);
 
        BadPig pig = go.GetComponent<BadPig>();
        if (pig != null)
        {
            if (pig.isNormallyInvincible)       // El Porkador: knocked back, not hurt
                pig.Stun(1f);
            else if (killPigsInBlast)
                pig.Stun(pig.hp * 2f + 1f);
        }
    }
 
    // Same rules the Mighty Eagle uses to decide what he obliterates
    bool IsDestroyable(GameObject go, Collider2D c)
    {
        if (go.GetComponent<GroundBlock>() != null)     // the ground can't be destroyed
            return false;
        bool isPig = go.GetComponent<BadPig>() != null
                  || (go.GetComponent<ABcharacters>() != null && go.GetComponent<AngryBird>() == null);
        bool isBlock = go.GetComponent<Block>() != null;
        if (isPig || isBlock)
            return true;
 
        if (c.attachedRigidbody == null)      // no rigidbody = the ground / static scenery
            return false;
        if (go.layer == 11)                   // the "ground" layer
            return false;
        if (go.CompareTag("Player"))          // birds, slingshot, egg, crown
            return false;
        if (go.GetComponent<AngryBird>() != null)
            return false;
        if (go.GetComponent<LimbBehaviour>() != null || go.GetComponent<DebrisComponent>() != null)   // people, debris
            return false;
        return true;
    }
}
public class CustomBird : MonoBehaviour
{
    // The player's settings. They are public fields so they are kept when the bird is saved.
    public int abilityIndex = 0;
    public float customMass = 2f;
    public float glass = 2f;
    public float wood = 2f;
    public float stone = 2f;
    public bool noGravity = false;
 
    // Tuning knobs
    public float keepAliveSeconds = 10f;   // the eagle abilities keep the bird from idle-despawning for this long
 
    // The numbered list. The order here must match the switch in AddAbility.
    public static readonly string[] AbilityNames = {
        "None (no ability)",        // 0
        "Red",                      // 1
        "Mighty Feathers Red",      // 2
        "Telebird",                 // 3
        "Homing Bird",              // 4
        "Chuck",                    // 5
        "Bomb",                     // 6
        "Shockwave",                // 7
        "The Blues",                // 8
        "Terence",                  // 9
        "Tony",                     // 10
        "Matilda",                  // 11
        "Hal",                      // 12
        "The Bubbles",              // 13
        "Stella",                   // 14
        "Ice Bird",                 // 15
        "Silver",                   // 16
        "Space Egg",                // 17
        "Mighty Eagle",             // 18
        "Space Eagle",              // 19
        "Super Red",                // 20
        "Lazer Bird"                // 21
    };
 
    AngryBird bird;
    SpaceFlight spaceFlight;
    bool applying = false;
    bool addedExplosive = false;
    bool keepAlive = false;
    float normalIdleDespawn = 4f;
 
    void Start()
    {
        bird = GetComponent<AngryBird>();
        if (bird == null)
            return;
        normalIdleDespawn = bird.idleDespawnTime;
 
        // One SpaceFlight for the whole life of the bird. Super Red and Lazer Bird only add their own if there is none,
        // so this is also what stops them from forcing zero gravity on the Custom Bird.
        spaceFlight = GetComponent<SpaceFlight>();
        if (spaceFlight == null)
            spaceFlight = gameObject.AddComponent<SpaceFlight>();
        ApplyGravityMode();
        ApplyStats();
        AddMenu();
 
        // A copy of a bird (The Blues clone themselves) already has its trigger: leave it alone
        if (GetComponent<UseEventTrigger>() == null)
            StartCoroutine(ApplyAbility());
    }
 
    // ------------------------------------------------------------
    // MENU
    // ------------------------------------------------------------
    void AddMenu()
    {
        PhysicalBehaviour p = GetComponent<PhysicalBehaviour>();
        if (p == null)
            return;
 
        p.ContextMenuOptions.Buttons.Add(new ContextMenuButton("CustomNext", "Custom: Next Ability", "Custom: Next Ability", () =>
        {
            SelectAbility(abilityIndex + 1);
        }));
        p.ContextMenuOptions.Buttons.Add(new ContextMenuButton("CustomPrevious", "Custom: Previous Ability", "Custom: Previous Ability", () =>
        {
            SelectAbility(abilityIndex - 1);
        }));
        p.ContextMenuOptions.Buttons.Add(new ContextMenuButton("CustomPick", "Custom: Pick Ability By Number", "Custom: Pick Ability By Number", () =>
        {
            Utils.OpenFloatInputDialog<CustomBird>((float)abilityIndex, this, (System.Action<CustomBird, float>)((t, v) => t.SelectAbility(Mathf.RoundToInt(v))), "Pick Ability By Number", string.Format("from {0} to {1} (use List Abilities to see them)", (object)0, (object)(AbilityNames.Length - 1)));
        }));
        p.ContextMenuOptions.Buttons.Add(new ContextMenuButton("CustomList", "Custom: List Abilities", "Custom: List Abilities", () =>
        {
            string all = "";
            for (int i = 0; i < AbilityNames.Length; i++)
                all += i + " " + AbilityNames[i] + (i < AbilityNames.Length - 1 ? ",  " : "");
            ModAPI.Notify(all);
        }));
        p.ContextMenuOptions.Buttons.Add(new ContextMenuButton("CustomMass", "Custom: Set Mass", "Custom: Set Mass", () =>
        {
            Utils.OpenFloatInputDialog<CustomBird>(customMass, this, (System.Action<CustomBird, float>)((t, v) => t.SetMass(v)), "Set Mass", string.Format("from {0} to {1}", (object)0.1f, (object)5000f));
        }));
        p.ContextMenuOptions.Buttons.Add(new ContextMenuButton("CustomGlass", "Custom: Set Glass Strength", "Custom: Set Glass Strength", () =>
        {
            Utils.OpenFloatInputDialog<CustomBird>(glass, this, (System.Action<CustomBird, float>)((t, v) => t.SetGlass(v)), "Set Glass Strength", string.Format("from {0} to {1}", (object)0f, (object)1000f));
        }));
        p.ContextMenuOptions.Buttons.Add(new ContextMenuButton("CustomWood", "Custom: Set Wood Strength", "Custom: Set Wood Strength", () =>
        {
            Utils.OpenFloatInputDialog<CustomBird>(wood, this, (System.Action<CustomBird, float>)((t, v) => t.SetWood(v)), "Set Wood Strength", string.Format("from {0} to {1}", (object)0f, (object)1000f));
        }));
        p.ContextMenuOptions.Buttons.Add(new ContextMenuButton("CustomStone", "Custom: Set Stone Strength", "Custom: Set Stone Strength", () =>
        {
            Utils.OpenFloatInputDialog<CustomBird>(stone, this, (System.Action<CustomBird, float>)((t, v) => t.SetStone(v)), "Set Stone Strength", string.Format("from {0} to {1}", (object)0f, (object)1000f));
        }));
        p.ContextMenuOptions.Buttons.Add(new ContextMenuButton("CustomGravity", "Custom: Toggle No-Gravity Flight", "Custom: Toggle No-Gravity Flight", () =>
        {
            noGravity = !noGravity;
            ApplyGravityMode();
            if (noGravity)
                ModAPI.Notify("No-gravity flight on (when shot from Slingshot X)");
            else
                ModAPI.Notify("No-gravity flight off");
        }));
        p.ContextMenuOptions.Buttons.Add(new ContextMenuButton("CustomShow", "Custom: Show Settings", "Custom: Show Settings", () =>
        {
            ModAPI.Notify("Ability: " + AbilityNames[abilityIndex] + "   Mass: " + customMass + "   Glass: " + glass + "   Wood: " + wood + "   Stone: " + stone + "   No gravity: " + noGravity);
        }));
    }
 
    // ------------------------------------------------------------
    // MASS, STRENGTHS, GRAVITY
    // ------------------------------------------------------------
    public void SetMass(float v)
    {
        customMass = Mathf.Clamp(v, 0.1f, 5000f);
        ApplyMass();
        ModAPI.Notify("Mass: " + customMass);
    }
 
    public void SetGlass(float v)
    {
        glass = Mathf.Clamp(v, 0f, 1000f);
        ApplyStats();
        ModAPI.Notify("Glass strength: " + glass);
    }
 
    public void SetWood(float v)
    {
        wood = Mathf.Clamp(v, 0f, 1000f);
        ApplyStats();
        ModAPI.Notify("Wood strength: " + wood);
    }
 
    public void SetStone(float v)
    {
        stone = Mathf.Clamp(v, 0f, 1000f);
        ApplyStats();
        ModAPI.Notify("Stone strength: " + stone);
    }
 
    void ApplyStats()
    {
        if (bird == null)
            return;
        bird.glassMultiplier = glass;
        bird.woodMultiplier = wood;
        bird.stoneMultiplier = stone;
        ApplyMass();
    }
 
    void ApplyMass()
    {
        if (bird == null)
            return;
        PhysicalBehaviour p = GetComponent<PhysicalBehaviour>();
        if (p != null)
        {
            p.InitialMass = customMass;
            p.TrueInitialMass = customMass;
        }
        bird.mass = customMass;                                           // what the bird returns to after a hit
        bird.gameMass = bird.isTerence ? customMass * 0.1f : customMass * 6f;   // the mass Slingshot X gives it when launched
        if (!bird.shotFromSlingshot && bird.rb != null)
            bird.rb.mass = customMass;                                    // not while it is flying: it keeps its flight mass
    }
 
    void ApplyGravityMode()
    {
        if (spaceFlight == null)
            return;
        spaceFlight.zeroGravityOnLaunch = noGravity;
        // The Space birds disappear after drifting for a while. A normal-gravity bird shouldn't get those time limits.
        spaceFlight.maxFlightTime = noGravity ? 15f : 100000f;
        spaceFlight.despawnAfterHit = noGravity ? 6f : 100000f;
    }
 
    // ------------------------------------------------------------
    // ABILITY
    // ------------------------------------------------------------
    public void SelectAbility(int index)
    {
        int n = AbilityNames.Length;
        abilityIndex = ((index % n) + n) % n;
        ModAPI.Notify("Ability: " + AbilityNames[abilityIndex]);
        if (!applying)
            StartCoroutine(ApplyAbility());
    }
 
    IEnumerator ApplyAbility()
    {
        applying = true;
        while (true)
        {
            int index = abilityIndex;
 
            RemoveAbility();
            yield return null;               // the old scripts are really gone after this frame
            AddAbility(index);
 
            // Wait for the new ability to hook up its "use" trigger (Homing Bird and "None" have none)
            if (index != 0 && index != 4)
            {
                float waited = 0f;
                while (waited < 1f && GetComponent<UseEventTrigger>() == null)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }
            }
            if (keepAlive)
                WrapKeepAlive();
 
            // Tell the bird script to put its "ability locked after a hit" check in front of the new trigger
            if (bird != null)
                bird.useGateInstalled = false;
            yield return null;
 
            if (index == abilityIndex)
                break;                       // the player didn't change it again in the meantime
        }
        applying = false;
    }
 
    void Kill<T>() where T : Component
    {
        T[] found = GetComponents<T>();
        for (int i = 0; i < found.Length; i++)
            if (found[i] != null)
                Destroy(found[i]);
    }
 
    void RemoveAbility()
    {
        Kill<UseEventTrigger>();
        Kill<Red>();
        Kill<MightyRed>();
        Kill<Telebird>();
        Kill<HomingBird>();
        Kill<Chuck>();
        Kill<Bomb>();
        Kill<Shockwave>();
        Kill<ShockwaveSparks>();
        Kill<Blue>();
        Kill<Terence>();
        Kill<Tony>();
        Kill<Matilda>();
        Kill<Hal>();
        Kill<Bubbles>();
        Kill<Stella>();
        Kill<Icebird>();
        Kill<Silver>();
        Kill<SpaceEgg>();
        Kill<MightyEagleCan>();
        Kill<SpaceEagleCan>();
        Kill<SuperRed>();
        Kill<LazerBird>();
        if (addedExplosive)
        {
            Kill<ExplosiveBehaviour>();
            addedExplosive = false;
        }
 
        // Things the old abilities left lying around
        foreach (HomingField f in FindObjectsOfType<HomingField>())
            if (f != null && f.owner == transform)
                Destroy(f.gameObject);
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform c = transform.GetChild(i);
            if (c.name == "BombGlowOverlay" || c.name == "BombGlowHalo")
                Destroy(c.gameObject);
        }
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = Color.white;
        if (bird != null)
        {
            bird.freeRotation = false;
            bird.idleDespawnTime = normalIdleDespawn;
        }
        keepAlive = false;
    }
 
    void AddAbility(int index)
    {
        if (bird == null)
            return;
 
        // Red and "None" have no real ability (no ability cloud on the trail)
        bird.noAbility = (index == 0 || index == 1);
        // These abilities still work after the bird has hit something (same as the original birds)
        bird.activeAfterCollide = (index == 6 || index == 7 || index == 13 || index == 18 || index == 19);
        keepAlive = (index == 18 || index == 19);
 
        switch (index)
        {
            case 0:
                break;
            case 1:
                gameObject.AddComponent<Red>();
                break;
            case 2:
                gameObject.AddComponent<MightyRed>();
                break;
            case 3:
                gameObject.AddComponent<Telebird>();
                break;
            case 4:
                gameObject.AddComponent<HomingBird>();
                break;
            case 5:
                gameObject.AddComponent<Chuck>();
                break;
            case 6:
                AddBomb();
                break;
            case 7:
                gameObject.AddComponent<Shockwave>();
                break;
            case 8:
                gameObject.AddComponent<Blue>();
                break;
            case 9:
                gameObject.AddComponent<Terence>();
                break;
            case 10:
                gameObject.AddComponent<Tony>();
                break;
            case 11:
                gameObject.AddComponent<Matilda>();
                break;
            case 12:
                gameObject.AddComponent<Hal>();
                break;
            case 13:
                gameObject.AddComponent<Bubbles>();
                break;
            case 14:
                gameObject.AddComponent<Stella>();
                break;
            case 15:
                gameObject.AddComponent<Icebird>();
                break;
            case 16:
                gameObject.AddComponent<Silver>();
                break;
            case 17:
                gameObject.AddComponent<SpaceEgg>();
                break;
            case 18:
                {
                    MightyEagleCan m = gameObject.AddComponent<MightyEagleCan>();
                    m.destroyCan = false;       // the eagle must not destroy the bird itself
                }
                break;
            case 19:
                {
                    SpaceEagleCan s = gameObject.AddComponent<SpaceEagleCan>();
                    s.destroyCan = false;
                }
                break;
            case 20:
                gameObject.AddComponent<SuperRed>();
                break;
            case 21:
                gameObject.AddComponent<LazerBird>();
                break;
        }
    }
 
    // Bomb needs an ExplosiveBehaviour set up exactly like the Bomb registration does it
    void AddBomb()
    {
        if (GetComponent<ExplosiveBehaviour>() == null)
        {
            ExplosiveBehaviour ex = gameObject.AddComponent<ExplosiveBehaviour>();
            ex.Range = 24f;
            ex.FragmentForce = 48f;
            ex.ShotExplodeChance = -1f;
            ex.BurnPower = 0f;
            ex.ExplodesOnFragmentHit = false;
            ex.CanExplodeMultipleTimes = true;
            ex.BigExplosion = true;
            ex.DestroyOnExplode = false;
            ex.ArmOnUse = true;
            ex.Delay = 0f;
            ex.TemperatureLimit = float.MaxValue;
            addedExplosive = true;
        }
        gameObject.AddComponent<Bomb>();
    }
 
    // The eagle abilities take a few seconds, so the bird must not "lie still and vanish" before the eagle arrives
    // (same job the CanBird script does for the cans)
    void WrapKeepAlive()
    {
        UseEventTrigger t = GetComponent<UseEventTrigger>();
        if (t == null || t.Action == null)
            return;
        var original = t.Action;
        t.Action = () =>
        {
            if (bird != null)
                bird.idleDespawnTime = float.MaxValue;
            CancelInvoke("RestoreIdleDespawn");
            Invoke("RestoreIdleDespawn", keepAliveSeconds);
            original();
        };
    }
 
    void RestoreIdleDespawn()
    {
        if (bird != null)
            bird.idleDespawnTime = normalIdleDespawn;
    }
}
public class IcedProp : MonoBehaviour
{
    PhysicalBehaviour phy;
    void Start()
    {
        phy = gameObject.GetComponent<PhysicalBehaviour>();
        phy.Temperature = -99;
        if (!GetComponent<LimbBehaviour>() && !GetComponent<AngryBird>() && !GetComponent<BadPig>())
        {
            GetComponent<SpriteRenderer>().color = new Color32(170, 170, 255, 255);
        }
        else
            Destroy(phy);
    }
    void Update()
    {
        phy.Temperature = -99;
    }
}
public class ABcharacters : MonoBehaviour
{
    public GameObject target;
    public float torque = 5, chargeTimer = 0, cognitionDistance = 10;
    public bool enableSelfBalancing = true;
    public Rigidbody2D rb;
    [SkipSerialisation]
    public CircleCollider2D cognitionCollider;
    public enum Status
    {
        Idle,
        SlingFly,
        Stun,
        Air
    }
    [SkipSerialisation]
    public Status stat = Status.Idle;
    public enum Personality
    {
        Coward,
        Brave,
        Idle
    }
    public Personality personality;
    public int personalityNum;
    public void Start1()
    {
        switch (personality)
        {
            case Personality.Brave: personalityNum = 1; break;
            case Personality.Coward: personalityNum = -1; break;
            case Personality.Idle: personalityNum = 0; break;
        }
        cognitionCollider = gameObject.AddComponent<CircleCollider2D>();
        cognitionCollider.radius = cognitionDistance;
        cognitionCollider.isTrigger = true;
    }
    public void SetPersonality(Personality p)
    {
        personality = p;
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (GetComponent<AngryBird>() && other.GetComponent<BadPig>())
        {
            target = other.gameObject;
        }
        if (GetComponent<BadPig>() && other.GetComponent<AngryBird>())
        {
            target = other.gameObject;
        }
        if (GetComponent<BadPig>() && other.GetComponent<Egg>())
        {
            target = other.gameObject;
        }
    }
    public void Update1()
    {
        if (enableSelfBalancing)
        {
            if (transform.eulerAngles.z < 180 && transform.eulerAngles.z > 0)
                rb.AddTorque(-Mathf.Abs(rb.mass * (transform.eulerAngles.z)) * torque * Time.deltaTime, ForceMode2D.Force);
            else
                rb.AddTorque(Mathf.Abs(rb.mass * (360 - transform.eulerAngles.z)) * torque * Time.deltaTime, ForceMode2D.Force);
        }
    }
    public void Action_AI()
    {
        chargeTimer -= Time.deltaTime;
        if (!target && cognitionCollider)
        {
            cognitionCollider.enabled = false;
            cognitionCollider.enabled = true;
        }
        if (target)
        {
            if (stat == Status.Idle)
            {
                if (Mathf.Abs(transform.position.x - target.transform.position.x) < 4 && chargeTimer < 0 && personalityNum == 1)
                {
                    rb.AddForce(rb.mass * 500 * Vector3.Normalize(-transform.position + target.transform.position), ForceMode2D.Force);
                    chargeTimer = UnityEngine.Random.Range(0.8f, 1.2f);
                }
                if ((Mathf.Abs(transform.position.x - target.transform.position.x) > 3 || personality == Personality.Coward) && rb.velocity.magnitude < 4)//far
                {
                    rb.velocity += new Vector2(Vector3.Normalize(-transform.position + target.transform.position).x * 10 * personalityNum * Time.deltaTime, Time.deltaTime * 15);
                }
                else if (rb.velocity.magnitude < 4 && personality == Personality.Brave)//near
                {
                    rb.velocity += new Vector2(Vector3.Normalize(transform.position - target.transform.position).x * 10 * personalityNum * Time.deltaTime, 0);
                }
            }
            if (transform.position.x > target.transform.position.x)
            {
                transform.localScale = new Vector2(-Mathf.Abs(transform.localScale.x) * personalityNum, transform.localScale.y);
            }
            if (transform.position.x < target.transform.position.x)
            {
                transform.localScale = new Vector2(Mathf.Abs(transform.localScale.x) * personalityNum, transform.localScale.y);
            }
        }
    }
}
public class AngryBird : ABcharacters
{
    [SkipSerialisation]
    public string codeName, codeName_sprite;
    [SkipSerialisation] 
    public AudioClip[] humming = { null, null, null, null, null, null, null, null };
    [SkipSerialisation]
    public AudioClip[] impact = { null, null, null, null, null, null, null, null };
    [SkipSerialisation]
    public AudioClip[] launch = { null, null, null, null, null, null, null, null };
    [SkipSerialisation]
    public AudioClip[] stretch = { null, null, null, null, null, null, null, null };
    [SkipSerialisation]
    public AudioClip[] use = { null, null, null, null, null, null, null, null };
    [SkipSerialisation]
    public Sprite[] sprite = { null, null, null };
    [SkipSerialisation]
    public AudioSource mouth;
    [SkipSerialisation]
    public int humming_length, impact_length, launch_length, stretch_length, use_length;
    Color32 personalColor = new Color32(0,0,0,0);
    [SkipSerialisation]
    public float timer = 0f, hummingTimer = 0.1f;
    [SkipSerialisation]
    public bool isTerence = false;
    public bool AI = false, death = true;
    public enum WireAction
    {
        None,
        Destroy,
        DestroyOnHighSpeed
    }
    public WireAction wireAction = WireAction.DestroyOnHighSpeed;
    [SkipSerialisation]
    float abilityTimer = 0;
    [SkipSerialisation]
    public Slingshot slingshot;
    [SkipSerialisation]
    public bool isMouseOn = false, missingVoice = false;
    public float mass, gameMass, glassMultiplier = 1, woodMultiplier = 1, stoneMultiplier = 1;
    public PhysicalBehaviour phy;
    // Despawn: a bird shot from Slingshot X disappears after lying (almost) still for a while
    public bool shotFromSlingshot = false;
    public float idleDespawnTime = 4f;     // seconds of lying still before it disappears
    public float idleDespawnSpeed = 2f;  // slower than this counts as "not moving"
    float stillTimer = 0f;
	// Ability lock: after a bird shot from Slingshot X hits something, its ability stops working
    public bool activeAfterCollide = false;   // true = the ability still works after hitting something
    public bool noAbility = false;            // true for birds that have no real ability (Red): no ability cloud on the trail
    public bool hasCollidedAfterShot = false;
    [SkipSerialisation]
    public bool useGateInstalled = false;
    public bool AbilityLocked
    {
        get { return shotFromSlingshot && hasCollidedAfterShot && !activeAfterCollide; }
    }
    public bool freeRotation = false;         // true while an ability is rotating the bird itself (switches off the "point along the flight" rule)
    void Start()
    {
        if(!GetComponent<ABcharacters>())
            gameObject.AddComponent<ABcharacters>();
        Start1();
        ModAPI.Notify("<color=green>Toggle death, AI option in the right click menu!</color>");
        //GetComponent<AudioSource>().volume = 0.5f;
        GetComponent<PhysicalBehaviour>().ContextMenuOptions.Buttons.Add(new ContextMenuButton("Toggle death", "Toggle death", "Toggle death", () =>
        {
            if (isTerence)
                ModAPI.Notify("Terence can't die.");
            else
            {
                death = !death;
                if (death)
                    ModAPI.Notify("death on");
                else
                    ModAPI.Notify("death off");
            }
        }));
        GetComponent<PhysicalBehaviour>().ContextMenuOptions.Buttons.Add(new ContextMenuButton("Toggle AI", "Toggle AI", "Toggle AI", () =>
        {
            AI = !AI;
            if (AI)
                ModAPI.Notify("AI on");
            else
                ModAPI.Notify("AI off");
        }));
        Destroy(GetComponent<BoxCollider2D>());
        gameObject.AddComponent<PolygonCollider2D>();
        gameObject.tag = "Player";
        sprite[0] = ModAPI2.LoadSprite(codeName_sprite + ".png");
        if (!isTerence)
            sprite[1] = ModAPI2.LoadSprite(codeName_sprite + "2.png");
        else
            sprite[1] = sprite[0];
        rb = GetComponent<Rigidbody2D>();
        InitializeVoice();
        mouth.maxDistance = 100f;
        mouth.rolloffMode = AudioRolloffMode.Linear;
        mouth.spatialBlend = 1f;
        mass = rb.mass;
        gameMass = mass * 6;
        PhysicsMaterial2D pm;
        pm = new PhysicsMaterial2D();
        pm.bounciness = 0;
        pm.friction = 1;
        rb.sharedMaterial = pm;
        rb.angularDrag = 2f;
        StartCoroutine("RecoverSprite");
        phy = GetComponent<PhysicalBehaviour>();
        var color = phy.Properties.ShotImpact.GetComponentsInChildren<ParticleSystem>()[0].main;
        color.startColor = Color.red;
        color = GetComponent<PhysicalBehaviour>().Properties.ShotImpact.GetComponentsInChildren<ParticleSystem>()[1].main;
        color.startColor = Color.red;
        color = GetComponent<PhysicalBehaviour>().Properties.ShotImpact.GetComponentsInChildren<ParticleSystem>()[2].main;
        color.startColor = Color.red;
        color = GetComponent<PhysicalBehaviour>().Properties.ShotImpact.GetComponentsInChildren<ParticleSystem>()[3].main;
        color.startColor = Color.red;
        GetComponent<PhysicalBehaviour>().Properties.ShotImpact.GetComponentInChildren<BloodImpactBehaviour>().SetColor(Color.red);
        if (isTerence)
        {
            gameMass = mass * 0.1f;
            GetComponent<PhysicalBehaviour>().Properties.ShotImpact = null;
        }
        switch (codeName_sprite)
        {
            case "Red":
                personalColor = new Color32(255, 0, 0, 255);
                break;
			case "MightyRed":
                personalColor = new Color32(255, 100, 100, 255);
                break;
			case "Telebird":
                personalColor = new Color32(170, 80, 255, 255);
                break;
            case "HomingBird":
                personalColor = new Color32(0, 255, 0, 255);
                break;
           case "SuperRed":
                personalColor = new Color32(255, 0, 0, 255);
                break;
                break;
            case "Chuck":
                personalColor = new Color32(255, 255, 0, 255);
                break;
            case "LazerBird":
                personalColor = new Color32(200, 100, 200, 255);
                break;
            case "Bomb":
                personalColor = new Color32(0, 0, 0, 255);
                break;
            case "SpaceBomb":
                personalColor = new Color32(240, 140, 0, 255);
                break;
            case "Shockwave":
                personalColor = new Color32(0, 200, 255, 255);
                break;
            case "Blues":
                personalColor = new Color32(0, 200, 255, 255);
                break;
            case "LightningBird":
                personalColor = new Color32(80, 200, 255, 255);
                break;
            case "Matilda":
                personalColor = new Color32(255, 255, 255, 255);
                break;
            case "Terence":
                personalColor = new Color32(200, 0, 0, 255);
                break;
            case "IncredibleTerence":
                personalColor = new Color32(80, 200, 80, 255);
                break;
            case "Tony":
                personalColor = new Color32(0, 200, 255, 255);
                break;
            case "Hal":
                personalColor = new Color32(0, 255, 0, 255);
                break;
            case "Bub":
                personalColor = new Color32(255, 220, 0, 255);
                break;
            case "Stella":
                personalColor = new Color32(255, 210, 210, 255);
                break;
            case "Icebird":
                personalColor = new Color32(0, 200, 255, 255);
                break;
            case "Silver":
                personalColor = new Color32(138, 152, 163, 255);
                break;
            case "SpaceEgg":
                personalColor = new Color32(200, 255, 255, 255);
				break;
            case "idontknowitsname":
                personalColor = new Color32(190, 190, 190, 255);
                break;
            case "CustomBird":
                personalColor = new Color32(140, 140, 140, 255);
                break;
            default:
                personalColor = new Color32(0, 255, 255, 255);
                break;
        }
        phy.RefreshOutline();
    }
    void OnMouseOver()
    {
        isMouseOn = true;
    }
    void OnMouseExit()
    {
        isMouseOn = false;
    }
    float rotZ;
    void Update()
    {
        if (shotFromSlingshot)
            enableSelfBalancing = false;
        Update1();
        if (AI)
        {
            Action_AI();
            Action_AI_BirdAbility();
        }
        if(stat == Status.Idle)
        {
            if (hummingTimer >= 0 && humming != null)
                hummingTimer -= Time.deltaTime;
            if (hummingTimer < 0)
            {
                mouth.clip = humming[UnityEngine.Random.Range(1, 4)];
                mouth.Play();
                hummingTimer = UnityEngine.Random.Range(5f, 20f);
            }
        }
        if (timer > 0)
            timer -= Time.deltaTime;
        if (timer < 0)
            Idle();
        if (phy.Temperature > 200 && !isTerence)
            Die();
        if (shotFromSlingshot && rb != null && rb.simulated)
        {
            if (rb.velocity.magnitude < idleDespawnSpeed)
            {
                stillTimer += Time.deltaTime;
                if (stillTimer >= idleDespawnTime)
                {
                    stillTimer = 0f;
                    Die();
                }
            }
            else
                stillTimer = 0f;
        }
        else
            stillTimer = 0f;
		// Put the ability lock in front of every "use" action this bird has (done once, after the ability scripts have set theirs up)
        if (!useGateInstalled)
        {
            UseEventTrigger[] triggers = GetComponents<UseEventTrigger>();
            for (int i = 0; i < triggers.Length; i++)
            {
                if (triggers[i].Action == null)
                    continue;
                var original = triggers[i].Action;
                triggers[i].Action = () =>
                {
                    if (AbilityLocked)
                    {
                        ModAPI.Notify("<color=red>Ability locked: this bird already hit something.</color>");
                        return;
                    }
                    original();
					BirdTrail trail = GetComponent<BirdTrail>();
                    if (trail != null && !noAbility)
                        trail.MarkAbility();
                };
                useGateInstalled = true;
            }
        }
    }
    void Action_AI_BirdAbility()
    {
        abilityTimer -= Time.deltaTime;
        if (!target)
            return;
        if (Mathf.Abs(transform.position.x - target.transform.position.x) < 4)
        {
            if (GetComponent<Bomb>() && abilityTimer < 0)
            {
                GetComponent<ExplosiveBehaviour>().Use(new ActivationPropagation());
                abilityTimer = UnityEngine.Random.Range(0.8f, 1.2f);
            }
            if (GetComponent<Chuck>() && abilityTimer < 0)
            {
                GetComponent<AngryBird>().rb.AddForce(Vector3.Normalize(target.transform.position - transform.position) * rb.mass * 20, ForceMode2D.Impulse);
                GetComponent<AngryBird>().mouth.clip = GetComponent<AngryBird>().use[UnityEngine.Random.Range(1, 4)];
                GetComponent<AngryBird>().mouth.Play();
                GetComponent<SpriteRenderer>().sprite = GetComponent<AngryBird>().sprite[2];
                GetComponent<AngryBird>().timer = 5f;
                ModAPI.CreateParticleEffect("Flash", transform.position);
                abilityTimer = UnityEngine.Random.Range(0.8f, 1.2f);
            }
            if (GetComponent<Stella>() && abilityTimer < 0)
            {
                GetComponent<UseEventTrigger>().Action();
                abilityTimer = UnityEngine.Random.Range(4.8f, 5.2f);
            }
            if (GetComponent<Bubbles>() && abilityTimer < 0)
            {
                GetComponent<UseEventTrigger>().Action();
                abilityTimer = UnityEngine.Random.Range(5.8f, 6.2f);
            }
        }
    }
	public void Vanish(float keepAlive)
    {
        Renderer[] rs = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < rs.Length; i++)
            rs[i].enabled = false;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }
        enabled = false;
        Destroy(gameObject, keepAlive);
    }

    void FixedUpdate()
    {
        if (stat == Status.SlingFly && !freeRotation)
        {
            rotZ = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, rotZ);
        }
    }
    void OnCollisionEnter2D(Collision2D c)
    {
        if (shotFromSlingshot)
            hasCollidedAfterShot = true;
        GameObject target = c.gameObject;
        if (stat == Status.SlingFly && !target.CompareTag("Player"))
        {
            if (target.GetComponent<DestroyableBehaviour>() && !target.GetComponent<Block>())
            {
                target.GetComponent<DestroyableBehaviour>().OverallChance = 10000;
                target.GetComponent<DestroyableBehaviour>().Break();
            }
            DestroyWires(c.gameObject);
            StartCoroutine("FlyingCollision");
        }
        if (wireAction == WireAction.Destroy)
        {
            DestroyWires(c.gameObject);
        }
        if (wireAction == WireAction.DestroyOnHighSpeed && rb.velocity.magnitude > 5)
        {
            DestroyWires(c.gameObject);
        }
        if (c.gameObject.GetComponent<Rigidbody2D>())
        {
            Rigidbody2D crb = c.gameObject.GetComponent<Rigidbody2D>();
            Stun(0.1f * Utility.Impact(rb, crb));
        }
        if(rb.velocity.magnitude > 5 && mass > 1 && rb.drag > 0)
        {
            if(!target.GetComponent<Block>())
            {
                target.GetComponent<DestroyableBehaviour>().OverallChance = 10000;
                target.GetComponent<DestroyableBehaviour>().Break();
            }
        }
    }
    public void DestroyWires(GameObject c)
    {
        Utility.DestroyWires(c);
    }
    IEnumerator FlyingCollision()
    {
        rb.drag = 1;
        stat = Status.Idle;
        yield return new WaitForSeconds(1);
        rb.mass = mass;
        rb.drag = 0;
    }
    IEnumerator RecoverSprite()
    {
        yield return new WaitForEndOfFrame();
        GetComponent<SpriteRenderer>().sprite = sprite[0];
    }
    public void Stun(float impactScale, bool terenceGetsAffected = false)
    {
        if (impactScale <= 0)
            return;
        mouth.clip = impact[UnityEngine.Random.Range(1, 4)];
        mouth.Play();
        if (isTerence && !terenceGetsAffected)
            return;
        gameObject.GetComponent<SpriteRenderer>().sprite = sprite[1];
        stat = Status.Stun;
        timer = Mathf.Min(impactScale * 0.5f, 5f);
        enableSelfBalancing = false;
    }
    void Idle()
    {
        if(!isTerence)
            gameObject.GetComponent<SpriteRenderer>().sprite = sprite[0];
        if(stat != Status.SlingFly)
            stat = Status.Idle;
        enableSelfBalancing = true;
    }
    public void Say(AudioClip[] a)
    {
        mouth.clip = a[UnityEngine.Random.Range(1, 4)];
        mouth.Play();
    }
    private void Shot(global::Shot shot)
    {
        Stun(shot.damage * 0.1f);
        if (shot.damage > 90 && !isTerence)
            Die();
    }
    public void Die()
    {
        if (!death)
            return;
        Utility.CreateFeatherEffect(personalColor, transform.position);
        Utility.CreateAnimation("Pop", 8, transform.position, personalColor);
        Utility.CreateOneShotAudio(ModAPI2.LoadSound("AudioClip/bird_destroyed.wav"), transform.position, 2);
        Destroy(gameObject);
    }
    private void InitializeVoice()
    {
        if (codeName != null)
        {
            launch[1] = ModAPI2.LoadSound("AudioClip/" + codeName + "_voice_release.wav");
            launch[2] = ModAPI2.LoadSound("AudioClip/" + codeName + "_voice_release_2.wav");
            launch[3] = ModAPI2.LoadSound("AudioClip/" + codeName + "_voice_release_3.wav");

            impact[1] = ModAPI2.LoadSound("AudioClip/" + codeName + "_voice_impact.wav");
            impact[2] = ModAPI2.LoadSound("AudioClip/" + codeName + "_voice_impact_2.wav");
            impact[3] = ModAPI2.LoadSound("AudioClip/" + codeName + "_voice_impact_3.wav");

            if(!missingVoice)
            {
                use[1] = ModAPI2.LoadSound("AudioClip/" + codeName + "_voice_ability.wav");
                use[2] = ModAPI2.LoadSound("AudioClip/" + codeName + "_voice_ability_2.wav");
                use[3] = ModAPI2.LoadSound("AudioClip/" + codeName + "_voice_ability_3.wav");

                if (ModAPI2.LoadSound("AudioClip/" + codeName + "_voice_stretch.wav") != null)
                    stretch[1] = ModAPI2.LoadSound("AudioClip/" + codeName + "_voice_stretch.wav");
                if (ModAPI2.LoadSound("AudioClip/" + codeName + "_voice_stretch_2.wav") != null)
                    stretch[2] = ModAPI2.LoadSound("AudioClip/" + codeName + "_voice_stretch_2.wav");
                if (ModAPI2.LoadSound("AudioClip/" + codeName + "_voice_stretch_3.wav") != null)
                    stretch[3] = ModAPI2.LoadSound("AudioClip/" + codeName + "_voice_stretch_3.wav");

                if (ModAPI2.LoadSound("AudioClip/" + codeName + "_voice_level_select.wav") != null)
                    humming[1] = ModAPI2.LoadSound("AudioClip/" + codeName + "_voice_level_select.wav");
                if (ModAPI2.LoadSound("AudioClip/" + codeName + "_voice_level_select_2.wav") != null)
                    humming[2] = ModAPI2.LoadSound("AudioClip/" + codeName + "_voice_level_select_2.wav");
                if (ModAPI2.LoadSound("AudioClip/" + codeName + "_voice_level_select_3.wav") != null)
                    humming[3] = ModAPI2.LoadSound("AudioClip/" + codeName + "_voice_level_select_3.wav");
            }
        }
    }
}
public class BadPig : ABcharacters
{
    [SkipSerialisation]
    public string codeName, codeName_sprite;
    [SkipSerialisation]
    public AudioClip[] humming = { null, null, null, null, null, null, null, null };
    [SkipSerialisation]
    public AudioClip[] impact = { null, null, null, null, null, null, null, null };
    [SkipSerialisation]
    public AudioClip[] terror = { null, null, null, null };
    [SkipSerialisation]
    public Sprite[] sprite = { null, null, null };
    [SkipSerialisation]
    public AudioSource mouth;
    [SkipSerialisation]
    public SpriteRenderer wound;
    [SkipSerialisation]
    public int humming_length, impact_length, terror_length;
    [SkipSerialisation]
    public float timer = 0f, hummingTimer = 0.1f;
    [SkipSerialisation]
    public float hp = 10, fullhp;
    [SkipSerialisation]
    public bool isNormallyInvincible = false;
    public bool death = true;
    public bool AI = false;
    public Egg egg;
    void Start()
    {
        if (!GetComponent<ABcharacters>())
            gameObject.AddComponent<ABcharacters>();
        Start1();
        GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Human");
        ModAPI.Notify("<color=green>Toggle AI, invincibility option in the right click menu!</color>");
        //GetComponent<AudioSource>().volume = 0.5f;

        fullhp = hp;
        wound = new GameObject().AddComponent<SpriteRenderer>();
        wound.transform.position = transform.position;
        wound.transform.rotation = transform.rotation;
        if (transform.localScale.x < 0)
            wound.transform.localScale = new Vector2(-1, 1);
        else
            wound.transform.localScale = new Vector2(1, 1);
        wound.transform.SetParent(gameObject.transform);
        wound.sortingOrder = GetComponent<SpriteRenderer>().sortingOrder + 1;
        GetComponent<PhysicalBehaviour>().ContextMenuOptions.Buttons.Add(new ContextMenuButton("Toggle death", "Toggle death", "Toggle death", () =>
        {
            death = !death;
            if (death)
                ModAPI.Notify("death on");
            else
                ModAPI.Notify("death off");
        }));
        GetComponent<PhysicalBehaviour>().ContextMenuOptions.Buttons.Add(new ContextMenuButton("Toggle AI", "Toggle AI", "Toggle AI", () =>
        {
            AI = !AI;
            if (AI)
                ModAPI.Notify("AI on");
            else
                ModAPI.Notify("AI off");
        }));
        Destroy(GetComponent<BoxCollider2D>());
        gameObject.AddComponent<PolygonCollider2D>();
        gameObject.tag = "Player";
        sprite[0] = ModAPI2.LoadSprite(codeName_sprite + ".png", 1);
        sprite[1] = ModAPI2.LoadSprite(codeName_sprite + "2.png", 1);
        sprite[2] = ModAPI2.LoadSprite(codeName_sprite + "_wound.png", 1);
        rb = GetComponent<Rigidbody2D>();
        InitializeVoice();
        mouth.maxDistance = 100f;
        mouth.rolloffMode = AudioRolloffMode.Linear;
        mouth.spatialBlend = 1f;
        PhysicsMaterial2D pm;
        pm = new PhysicsMaterial2D();
        pm.bounciness = 0;
        pm.friction = 1;
        rb.sharedMaterial = pm;
        rb.angularDrag = 2f; var color = GetComponent<PhysicalBehaviour>().Properties.ShotImpact.GetComponentsInChildren<ParticleSystem>()[0].main;
        color.startColor = Color.red;
        color = GetComponent<PhysicalBehaviour>().Properties.ShotImpact.GetComponentsInChildren<ParticleSystem>()[1].main;
        color.startColor = Color.red;
        color = GetComponent<PhysicalBehaviour>().Properties.ShotImpact.GetComponentsInChildren<ParticleSystem>()[2].main;
        color.startColor = Color.red;
        color = GetComponent<PhysicalBehaviour>().Properties.ShotImpact.GetComponentsInChildren<ParticleSystem>()[3].main;
        color.startColor = Color.red;
        GetComponent<PhysicalBehaviour>().Properties.ShotImpact.GetComponentInChildren<BloodImpactBehaviour>().SetColor(Color.red);
        GetComponent<PhysicalBehaviour>().RefreshOutline();
    }

    void Update()
    {
        Update1();
        if (AI)
            Action_AI();
        if(rb.velocity.magnitude > 10)
        {
            stat = Status.Air;
            GetComponent<SpriteRenderer>().sprite = sprite[1];
            timer += Time.deltaTime;
            if(!mouth.isPlaying)
            {
                mouth.clip = terror[UnityEngine.Random.Range(1, 4)];
                mouth.Play();
            }
        }
        if (stat == Status.Idle)
        {
            if (transform.eulerAngles.z < 180 && transform.eulerAngles.z > 0)
                rb.AddTorque(-Mathf.Abs(rb.mass * (transform.eulerAngles.z)) * torque * Time.deltaTime, ForceMode2D.Force);
            else
                rb.AddTorque(Mathf.Abs(rb.mass * (360 - transform.eulerAngles.z)) * torque * Time.deltaTime, ForceMode2D.Force);
            if (hummingTimer >= 0)
                hummingTimer -= Time.deltaTime;
            if (hummingTimer < 0)
            {
                mouth.clip = humming[UnityEngine.Random.Range(1, 4)];
                mouth.Play();
                hummingTimer = UnityEngine.Random.Range(5f, 20f);
            }
        }
        if (timer > 0)
            timer -= Time.deltaTime;
        if (timer < 0)
            Idle();
        if (GetComponent<PhysicalBehaviour>().Temperature > 200)
            Die();
    }
    void OnCollisionEnter2D(Collision2D c)
    {
        if (isNormallyInvincible && c.gameObject.GetComponent<AngryBird>())
        {
            if (c.gameObject.GetComponent<AngryBird>().isTerence)
                hp -= 40;
            return;
        }
        if (c.gameObject.GetComponent<Rigidbody2D>())
        {
            Rigidbody2D crb = c.gameObject.GetComponent<Rigidbody2D>();
            if (c.gameObject.GetComponent<BadPig>())
                return;
            Stun(0.1f * Utility.Impact(rb, crb));
        }
    }
    public void Stun(float impactScale, bool terenceGetsAffected = false)
    {
        if (impactScale <= 0)
            return;
        mouth.clip = impact[UnityEngine.Random.Range(1, 4)];
        mouth.Play();
        gameObject.GetComponent<SpriteRenderer>().sprite = sprite[1];
        stat = Status.Stun;
        timer = Mathf.Min(impactScale * 0.5f, 5f);
        if (death)
        {
            if(AI)
                hp -= impactScale * 0.5f;
            else hp -= impactScale;
        }
        if (hp < fullhp * 0.5f)
        {
            wound.sprite = sprite[2];
        }
        if (death && hp < 0)
        {
            Die();
        }
        enableSelfBalancing = false;
    }
    static AudioClip pigDieClip = null;
    static bool pigDieWarned = false;
 
    void PlayDeathSound()
    {
        if (pigDieClip == null)
        {
            AudioClip c = ModAPI2.LoadSound("AudioClip/pig_die.wav");
            if (c != null && c.length > 0.01f)
                pigDieClip = c;
        }
        if (pigDieClip == null)
        {
            if (!pigDieWarned)
            {
                pigDieWarned = true;
                ModAPI.Notify("<color=red>pig_die.wav didn't load</color>");
            }
            return;
        }
 
        // The sound gets its own object with its own AudioSource, because the pig is destroyed right away and would cut it off
        GameObject g = new GameObject("PigDieSound");
        g.transform.position = transform.position;
        AudioSource s = g.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = false;
        s.mute = false;
        s.volume = 0.7f;
        s.maxDistance = 100f;
        s.rolloffMode = AudioRolloffMode.Linear;
        s.spatialBlend = 1f;
        s.PlayOneShot(pigDieClip);
        Destroy(g, pigDieClip.length + 0.5f);
    }
    void Die()
    {
        PlayDeathSound();
		Utility.CreateAnimation("Pop", 8, transform.position, new Color32(100, 255, 100, 255));
        Utility.CreateImage(ModAPI2.LoadSprite("5000.png"), transform.position + new Vector3(0, 1.5f));
		ModAPI2.LoadSound("AudioClip/pig_die.wav");
        Destroy(gameObject);
    }
    void Idle()
    {
        stat = Status.Idle;
        enableSelfBalancing = true;
        gameObject.GetComponent<SpriteRenderer>().sprite = sprite[0];
    }
    private void Shot(global::Shot shot)
    {
        Stun(shot.damage * 0.1f);
    }
    private void InitializeVoice()
    {
        if (codeName != null)
        {
            impact[1] = ModAPI2.LoadSound("AudioClip/" + codeName + "_death.wav");
            for (int i = 2; i <= 3; i++)
            {
                if (ModAPI2.LoadSound("AudioClip/" + codeName + "_death_" + i.ToString() + ".wav") != null)
                {
                    impact[i] = ModAPI2.LoadSound("AudioClip/" + codeName + "_death_" + i.ToString() + ".wav");
                    impact_length = i;
                }
            }

            humming[1] = ModAPI2.LoadSound("AudioClip/" + codeName + "_giggle_long.wav");
            for (int i = 2; i <= 3; i++)
            {
                humming[i] = ModAPI2.LoadSound("AudioClip/" + codeName + "_giggle_long_" + i.ToString() + ".wav");
                humming_length = i;
            }

            terror[1] = ModAPI2.LoadSound("AudioClip/" + codeName + "_terror.wav");
            terror[2] = ModAPI2.LoadSound("AudioClip/" + codeName + "_terror_2.wav");
            terror[3] = ModAPI2.LoadSound("AudioClip/" + codeName + "_terror_3.wav");
        }
    }
}
public class ConstructionPig : MonoBehaviour
{
    BadPig pig;
    float timer = 0;
    void Start()
    {
        pig = GetComponent<BadPig>();
    }
    void Update()
    {
        if (timer >= 0)
            timer -= Time.deltaTime;
        if (timer < 0 && pig.target != null)
            Throw(pig.target.transform.position);
    }
    void Throw(Vector2 target)
    {
        timer = 8;
        GameObject screwdriver = Instantiate(ModAPI.FindSpawnable("Brick").Prefab);
        screwdriver.transform.position = transform.position + Vector3.Normalize((Vector3)target - transform.position);
        screwdriver.GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("driver.png", 1f);
        screwdriver.GetComponent<Rigidbody2D>().gravityScale = 0;
        screwdriver.AddComponent<Screwdriver>().goalPos = target;
        screwdriver.GetComponent<Collider2D>().isTrigger = true;
    }
}
public class Screwdriver : MonoBehaviour
{
    float velocity = 20;
    public Vector2 goalPos;
    void Start() => GetComponent<Rigidbody2D>().velocity = Vector3.Normalize((Vector3)goalPos - transform.position) * velocity;
    void OnTriggerEnter2D(Collider2D c)
    {
        if (c.isTrigger)
            return;
        if (c.gameObject.GetComponent<AngryBird>())
        {
            c.gameObject.GetComponent<AngryBird>().Stun(2);
            c.gameObject.GetComponent<AngryBird>().rb.AddForce(Vector3.Normalize(c.transform.position - transform.position) * 40, ForceMode2D.Impulse);
        }
        ModAPI.CreateParticleEffect("Ricochet", transform.position);
        Destroy(gameObject);
    }
}
public class ElPorkador : MonoBehaviour
{
    void OnCollisionEnter2D(Collision2D c)
    {
        if(c.gameObject.GetComponent<AngryBird>())
        {
            AngryBird bird = c.gameObject.GetComponent<AngryBird>();
            bird.rb.AddForce(Vector3.Normalize(bird.transform.position - transform.position) * 20, ForceMode2D.Impulse);
            bird.Stun(10);
        }
    }
}
// Turns a BadPig into a boss: makes it bigger and gives it a floating health bar
public class BossPig : MonoBehaviour
{
    public float sizeMultiplier = 1.8f;                                   // how much bigger than the normal pig
    public float barHeight = 0.25f;                                       // thickness of the health bar (world units)
    public float barMinWidth = 2.5f;                                      // narrowest the bar can be
    public float barMaxWidth = 8f;                                        // widest the bar can be
    public float barGap = 0.5f;                                           // gap between the top of the pig and the bar
    public float barSmoothing = 4f;                                       // how quickly the bar slides to the new health (bigger = faster)
 
    BadPig pig;
    SpriteRenderer body;
    GameObject barRoot;
    Transform fill;
    SpriteRenderer fillRenderer;
    float width = 3f;
    float shown = 1f;                                                     // the health fraction the bar currently shows
    static Sprite whiteSprite = null;
 
    void Start()
    {
        pig = GetComponent<BadPig>();
        body = GetComponent<SpriteRenderer>();
 
        // Bigger (the collider is made from the sprite, so it grows with the pig; the wound sprite is a child and grows too)
        Vector3 s = transform.localScale;
        transform.localScale = new Vector3(s.x * sizeMultiplier, s.y * sizeMultiplier, s.z);
 
        BuildBar();
    }
 
    // A 1 x 1 white square whose left edge is its pivot, so scaling it horizontally fills the bar from the left
    static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null)
            return whiteSprite;
        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0f, 0.5f), 1f);
        return whiteSprite;
    }
 
    GameObject MakePart(string partName, Transform parent, Color color, int order)
    {
        GameObject g = new GameObject(partName);
        g.transform.SetParent(parent, false);
        SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = GetWhiteSprite();
        sr.color = color;
        sr.sortingOrder = order;
        return g;
    }
 
    void BuildBar()
    {
        // Not a child of the pig, so it never tilts or stretches with it (it's removed in OnDestroy)
        barRoot = new GameObject("BossHealthBar");
        int baseOrder = body != null ? body.sortingOrder + 100 : 200;
 
        width = Mathf.Clamp(body != null ? body.bounds.size.x : 3f, barMinWidth, barMaxWidth);
        float border = 0.06f;
 
        // Black border, dark red back, then the coloured fill on top
        GameObject frame = MakePart("Frame", barRoot.transform, new Color(0f, 0f, 0f, 0.9f), baseOrder);
        frame.transform.localPosition = new Vector3(-width * 0.5f - border, 0f, 0f);
        frame.transform.localScale = new Vector3(width + border * 2f, barHeight + border * 2f, 1f);
 
        GameObject back = MakePart("Back", barRoot.transform, new Color(0.25f, 0.05f, 0.05f, 1f), baseOrder + 1);
        back.transform.localPosition = new Vector3(-width * 0.5f, 0f, 0f);
        back.transform.localScale = new Vector3(width, barHeight, 1f);
 
        GameObject f = MakePart("Fill", barRoot.transform, Color.green, baseOrder + 2);
        f.transform.localPosition = new Vector3(-width * 0.5f, 0f, 0f);
        f.transform.localScale = new Vector3(width, barHeight, 1f);
        fill = f.transform;
        fillRenderer = f.GetComponent<SpriteRenderer>();
    }
 
    void LateUpdate()
    {
        if (barRoot == null || pig == null || body == null)
            return;
 
        // Where the bar floats: above the top of the pig, never rotated
        Bounds b = body.bounds;
        barRoot.transform.position = new Vector3(b.center.x, b.max.y + barGap, transform.position.z);
        barRoot.transform.rotation = Quaternion.identity;
 
        float target = pig.fullhp > 0f ? Mathf.Clamp01(pig.hp / pig.fullhp) : 0f;
        shown = Mathf.Lerp(shown, target, 1f - Mathf.Exp(-barSmoothing * Time.deltaTime));
        if (Mathf.Abs(shown - target) < 0.001f)
            shown = target;
 
        fill.localScale = new Vector3(Mathf.Max(0.0001f, width * shown), barHeight, 1f);
 
        // Green -> yellow -> red as the health runs out
        Color c = shown > 0.5f
            ? Color.Lerp(Color.yellow, Color.green, (shown - 0.5f) * 2f)
            : Color.Lerp(Color.red, Color.yellow, shown * 2f);
        fillRenderer.color = c;
    }
 
    void OnDestroy()
    {
        if (barRoot != null)
            Destroy(barRoot);
    }
}
public class PlanetPig : MonoBehaviour
{
    public CircleCollider2D air;
    public GameObject air_renderer;
    float range, rad;
    public float gravity;
    void Start()
    {
        GetComponent<SpriteRenderer>().sortingOrder = -1;
        air = gameObject.AddComponent<CircleCollider2D>();
        air_renderer = new GameObject();
        air_renderer.AddComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("Planet_air.png");
        air_renderer.GetComponent<SpriteRenderer>().sortingOrder = 0;
        air_renderer.transform.parent = transform;
        air_renderer.transform.localPosition = Vector3.zero;
        air_renderer.transform.localScale = transform.localScale;
        air.isTrigger = true;
        air.radius = 11.42f;
        rad = air.radius;
        if (!GetComponent<FreezeBehaviour>())
            gameObject.AddComponent<FreezeBehaviour>();
        GetComponent<PhysicalBehaviour>().ContextMenuOptions.Buttons.Add(new ContextMenuButton("Set Gravity Radius", "Set Gravity Radius", "Set Gravity Radius", () =>
        {
            Utils.OpenFloatInputDialog<PlanetPig>(range, this, (System.Action<PlanetPig, float>)((t, v) => t.range = Mathf.Clamp(v, 0.00f, 99999f)), "Set Gravity Radius (relative)", string.Format("from {0}s to {1}s", (object)0.00f, (object)99999f));
            air.radius = 11.42f * range * transform.localScale.x;
            air_renderer.transform.localScale = Vector2.one * range;
            rad = air.radius;
        }));
        GetComponent<PhysicalBehaviour>().ContextMenuOptions.Buttons.Add(new ContextMenuButton("Set Gravity Scale", "Set Gravity Scale", "Set Gravity Scale", () =>
        {
            Utils.OpenFloatInputDialog<PlanetPig>(gravity, this, (System.Action<PlanetPig, float>)((t, v) => t.gravity = Mathf.Clamp(v, 0.00f, 99999f)), "Set Gravity Scale", string.Format("from {0}s to {1}s", (object)0.00f, (object)99999f));
        }));
    }
    void OnTriggerStay2D(Collider2D c)
    {
        if (c.isTrigger)
            return;
        if(c.GetComponent<Rigidbody2D>() && !c.GetComponent<Temp>())
        {
            c.gameObject.AddComponent<Temp>();
            c.gameObject.GetComponent<Temp>().root = GetComponent<Rigidbody2D>();
        }
    }
    void OnTriggerExit2D(Collider2D c)
    {
        if (c.isTrigger)
            return;
        if (c.GetComponent<Rigidbody2D>())
        {
            Destroy(c.gameObject.GetComponent<Temp>());
        }
    }
    void OnDestroy()
    {
        foreach(Temp temp in GameObject.FindObjectsOfType<Temp>())
        {
            Destroy(temp);
        }
    }
}
public class Temp : MonoBehaviour
{
    public Rigidbody2D crb, root;
    PlanetPig planet;
    GameObject env;
    void Start()
    {
        crb = GetComponent<Rigidbody2D>();
        planet = root.GetComponent<PlanetPig>();
    }
    void Update()
    {
        crb.AddForce(crb.mass * planet.gravity * Time.deltaTime * Vector3.Normalize(root.transform.position - crb.transform.position) + Mathf.Abs(Physics2D.gravity.y) * crb.gravityScale * crb.mass * Vector3.up * Time.deltaTime, ForceMode2D.Impulse);
    }
}
public class Slingshot : MonoBehaviour
{
    public AudioSource slingSound;
    Vector2 localCenter;
    AngryBird bird, lastShotBird;
    GameObject branch_f, branch_b;
    AudioClip stretch, fire;
    bool readyToFire = false;
    GameObject[] guides = {null,null,null,null};
    void Start()
    {
        gameObject.AddComponent<AudioSourceTimeScaleBehaviour>();
        Destroy(GetComponent<BoxCollider2D>());
        branch_b = new GameObject();
        branch_f = new GameObject();
        branch_f.transform.SetParent(transform);
        branch_b.transform.SetParent(transform);
        branch_f.transform.localPosition = Vector2.zero;
        branch_b.transform.localPosition = Vector2.zero;
        branch_b.AddComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("slingshot_branch_b.png", 1f);
        branch_f.AddComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("slingshot_branch_f.png", 1f);
        branch_b.GetComponent<SpriteRenderer>().sortingOrder = GetComponent<SpriteRenderer>().sortingOrder - 1;
        branch_f.GetComponent<SpriteRenderer>().sortingOrder = GetComponent<SpriteRenderer>().sortingOrder + 1;
        if (slingSound == null)
            slingSound = gameObject.AddComponent<AudioSource>();
        else
            slingSound = GetComponent<AudioSource>();
        slingSound.loop = true;
        stretch = ModAPI2.LoadSound("AudioClip/slingshot_stretch_hard.wav");
        fire = ModAPI2.LoadSound("AudioClip/slingshot_fire.wav");
        slingSound.maxDistance = 100f;
        slingSound.rolloffMode = AudioRolloffMode.Linear;
        slingSound.spatialBlend = 1f;
        for (int i = 0; i < 4; i++)
        {
            guides[i] = new GameObject();
            guides[i].AddComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("guide.png", 1f);
            guides[i].transform.SetParent(transform);
        }
    }
    void OnCollisionEnter2D(Collision2D c)
    {
		if (bird != null)
            return;
        if (c.gameObject.GetComponent<AngryBird>())
        {
            lastShotBird = null;
            bird = c.gameObject.GetComponent<AngryBird>();
            bird.transform.SetParent(transform);
            if (bird.isTerence)
                localCenter = new Vector2(0, 0.6f);
            else
                localCenter = new Vector2(0, 0.1f);
            branch_b.GetComponent<SpriteRenderer>().sortingOrder = bird.GetComponent<SpriteRenderer>().sortingOrder - 1;
            branch_f.GetComponent<SpriteRenderer>().sortingOrder = bird.GetComponent<SpriteRenderer>().sortingOrder + 1;
            GetComponent<SpriteRenderer>().sortingOrder = bird.GetComponent<SpriteRenderer>().sortingOrder + 1;
            slingSound.clip = stretch;
            slingSound.volume = 0.1f;
            bird.rb.mass = bird.mass;
        }
    }
    Vector2 newPos;
    float rotZ;
    void Update()
    {
        if (bird)
        {
            if (Input.GetMouseButtonUp(0) && bird && slingSound.clip == stretch && slingSound.isPlaying && readyToFire)
                Launch();
        }
        if (lastShotBird && InputSystem.Down("Activate last shot bird"))
            lastShotBird.GetComponent<UseEventTrigger>().Action();
    }
    void FixedUpdate()
    {
        if(bird)
        {
            bird.rb.WakeUp();
            GetComponent<Rigidbody2D>().WakeUp();
            if (SelectionController.Main.SelectedObjects.Count != 0 && SelectionController.Main.SelectedObjects[0] == bird.GetComponent<PhysicalBehaviour>() && Input.GetMouseButton(0))
            {
                newPos = localCenter - (Vector2)bird.transform.localPosition;
                rotZ = Mathf.Atan2(newPos.y, newPos.x) * Mathf.Rad2Deg;
                bird.transform.rotation = Quaternion.Euler(0, 0, rotZ);
                readyToFire = true;
                if (!slingSound.isPlaying)
                    slingSound.Play();
                if (!bird.mouth.isPlaying && bird.stretch != null)
                    bird.Say(bird.stretch);
                for(int i = 0;i<4;i++)
                {
                    guides[i].SetActive(true);
                    guides[i].transform.localPosition = (Vector3)localCenter + Vector3.Normalize(newPos) * (i + 1);
                }
            }
            else //idle slingshot
            {
                for (int i = 0; i < 4; i++)
                {
                    guides[i].SetActive(false);
                }
                bird.transform.localPosition = localCenter;
                bird.transform.localRotation = Quaternion.Euler(0, 0, 0);
                bird.rb.velocity = Vector2.zero;
                //if (readyToFire)
                //    Launch();
            }
        }
        else
        {
            for (int i = 0; i < 4; i++)
            {
                guides[i].SetActive(false);
            }
        }
    }
    void Launch()
    {
        readyToFire = false;
        bird.transform.localRotation = Quaternion.Euler(0,0,0);
        slingSound.Stop();
        slingSound.volume = 0.3f;
        slingSound.PlayOneShot(fire);
        ModAPI.Notify("<color=green>Press T to activate last shot birds.</color>");
        bird.rb.AddForce(32 * bird.rb.mass * Vector3.Normalize(newPos), ForceMode2D.Impulse);
        bird.Say(bird.launch);
        bird.stat = AngryBird.Status.SlingFly;
        if (bird.isTerence)
            bird.GetComponent<Terence>().firTimeToStep = true;
        bird.rb.mass = bird.gameMass;
        bird.transform.localPosition = localCenter;
        bird.transform.parent = null;
        lastShotBird = bird;
        bird = null;
    }
}
public class SlingshotX : MonoBehaviour
{
    // ---- Pulling back ----
    public float maxPull = 3f;            // how far the bird can be pulled back (world units)
    public float minPull = 0.4f;          // pull less than this and letting go just cancels the shot
    public float grabRadius = 2f;       // click this close to the ready bird to grab it
    public float launchSpeed = 30f;       // speed at a full pull; every bird gets the same speed for the same pull
    public float minSpeedFraction = 0.2f; // even a small pull launches at least this fraction of launchSpeed
    public bool rotateWhilePulling = true;// the bird turns to face where it will fly (his sprite is assumed to face right)
    public bool activateLaunches = false; // true: the Activate key also fires straight along launchAngle at full speed
    public float launchAngle = 45f;       // only used when activateLaunches is true (degrees, in the slingshot's own space)
    public bool showGuide = true;         // dotted flight path while you're pulling
    public int guideDots = 8;
    public float guideStep = 0.04f;       // seconds of flight between dots (bigger = longer guide)
    public float guideLengthMultiplier = 1f;               // how many times more dots the guide has (the Sling Scope sets this)
    public Color guideColor = Color.white;                 // colour of the guide dots (the Sling Scope turns it red)
    Color dotsBuiltColor = Color.white;                    // (remembers what the current dots were built with)
 
    // ---- Sounds ----
    public float stretchVolume = 0.1f;    // the looping "slingshot_stretch_hard.wav" while pulling
    public float fireVolume = 0.3f;       // "slingshot_fire.wav" on release
 
    // ---- Where birds sit (in the slingshot's own space; mirrors if the slingshot is flipped) ----
    public Vector2 cupOffset = new Vector2(0f, 0f);       // the bird that's ready to fire
    public int maxQueued = 7;            // how many can wait on the ground (so 8 birds in total, counting the cup)
    public float maxWaitDistance = 28f;  // a waiting bird that gets knocked/walks further away than this is forgotten
 
    // ---- Reloading ----
    public float reloadDelay = 1.5f;     // seconds after a shot before the next bird hops on
    public float hopTime = 0.7f;        // how long a hop takes
    public float hopHeight = 2f;       // how high a hop arcs
	
    // ---- Waiting birds fidget ----
    public bool idleTricks = true;        // waiting birds jump now and then
    public float idleJumpMin = 4f;        // seconds between one bird's jumps: random between these
    public float idleJumpMax = 14f;
    public float idleJumpSpeed = 4f;      // how high they jump
    public float idleSpinChance = 0.2f;  // chance that a jump is a flip (a full spin in the air)
    public float hopSpinChance = 0.4f;    // chance that the hop into the cup is a spin
 
    // ---- Misc ----
    public bool activateOnce = true;     // X uses the last shot bird's power once
    public float catchDistance = 0.1f;   // how close a bird must be to the slingshot's body to be caught
 
    // The bird that's in the cup (or hopping into it)
    class Rec
    {
        public AngryBird bird;
        public Vector2 cur;       // where it is, in the slingshot's own space
        public Vector2 from;      // where its current hop started
        public Vector2 target;    // where it's heading
        public float t = 1f;      // hop progress, 0..1 (1 = arrived)
        public float spinTurns = 0f;   // spins during the hop into the cup: 0 = none, 1 / -1 = one full turn either way
    }
 
    Rec cup = null;
    // Waiting birds are NOT frozen: they stay on the ground as normal physics objects
    List<AngryBird> waiting = new List<AngryBird>();
    Dictionary<AngryBird, float> nextIdleJump = new Dictionary<AngryBird, float>();
    Dictionary<AngryBird, float> blockedUntil = new Dictionary<AngryBird, float>();   // so a just-shot bird isn't re-caught
    AngryBird lastShot = null;
    bool lastShotUsed = false;
    float reloadTimer = 0f;
    float scanTimer = 0f;
    bool hinted = false;
 
    // Pulling
    bool pulling = false;
    Vector2 pull = Vector2.zero;          // from the cup toward where the bird is being held (world units)
    RigidbodyType2D savedBodyType = RigidbodyType2D.Dynamic;
 
    // Sounds
    AudioSource slingSound = null;
    AudioClip stretchClip = null;
    AudioClip fireClip = null;
 
    GameObject[] dots = null;
    static Sprite dotSprite = null;
	
    // Loads a clip and makes sure it really loaded (a missing/broken file gives null or a zero-length clip)
    static AudioClip LoadClip(string file)
    {
        AudioClip c = ModAPI2.LoadSound("AudioClip/" + file);
        if (c != null && c.length > 0.01f)
            return c;
        return null;
    }
	
    // Every waiting bird jumps at its own random times; sometimes the jump is a flip
    void UpdateIdleTricks()
    {
        if (!idleTricks)
            return;
 
        foreach (AngryBird b in waiting)
        {
            if (b == null)
                continue;
 
            float next = 0f;
            if (!nextIdleJump.TryGetValue(b, out next))
            {
                nextIdleJump[b] = Time.time + UnityEngine.Random.Range(idleJumpMin, idleJumpMax);
                continue;
            }
            if (Time.time < next)
                continue;
            nextIdleJump[b] = Time.time + UnityEngine.Random.Range(idleJumpMin, idleJumpMax);
 
            // only a calm bird that's standing still on the ground
            Rigidbody2D rb = b.GetComponent<Rigidbody2D>();
            if (rb == null || !rb.simulated || b.stat != AngryBird.Status.Idle)
                continue;
            if (Mathf.Abs(rb.velocity.y) > 0.3f || rb.velocity.magnitude > 2f)
                continue;
 
            StartCoroutine(IdleJump(b, rb, UnityEngine.Random.value < idleSpinChance));
        }
    }
 
    IEnumerator IdleJump(AngryBird b, Rigidbody2D rb, bool spin)
    {
        float g = Mathf.Max(1f, -Physics2D.gravity.y * rb.gravityScale);
        float airtime = 2f * idleJumpSpeed / g;
 
        rb.velocity = new Vector2(rb.velocity.x + UnityEngine.Random.Range(-1.5f, 1.5f), idleJumpSpeed);
        if (spin)
        {
            // one full turn over the time in the air; no stand-upright torque while he flips
            float dir = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            b.enableSelfBalancing = false;
            rb.angularVelocity = dir * 360f / airtime;
        }
 
        yield return new WaitForSeconds(airtime + 0.15f);
 
        if (b != null && spin && !b.shotFromSlingshot)
            b.enableSelfBalancing = true;
    }
	
    // The bird that's in the cup (or hopping into it), or null if there isn't one. Power ups use this.
    public AngryBird CupBird
    {
        get { return cup != null ? cup.bird : null; }
    }
 
    void Start()
    {
        // The Activate key (only fires if activateLaunches is on)
        if (!gameObject.GetComponent<TriggerEventBehaviour>())
        {
            gameObject.AddComponent<UseEventTrigger>().Action = () =>
            {
                if (activateLaunches)
                    LaunchCup(FixedDirection(), launchSpeed);
            };
        }
 
        // Sounds: same setup as the old Slingshot
        if (GetComponent<AudioSourceTimeScaleBehaviour>() == null)
            gameObject.AddComponent<AudioSourceTimeScaleBehaviour>();
        slingSound = gameObject.AddComponent<AudioSource>();
        slingSound.loop = true;
        slingSound.playOnAwake = false;
        slingSound.mute = false;
        slingSound.volume = 1f;
        slingSound.maxDistance = 100f;
        slingSound.rolloffMode = AudioRolloffMode.Linear;
        slingSound.spatialBlend = 1f;
        stretchClip = LoadClip("slingshot_stretch_hard.wav");
        fireClip = LoadClip("slingshot_fire.wav");
        if (stretchClip == null)
            ModAPI.Notify("<color=red>slingshot_stretch_hard.wav didn't load</color>");
        if (fireClip == null)
            ModAPI.Notify("<color=red>slingshot_fire.wav didn't load</color>");
    }
 
    // ------------------------------------------------------------
    // CATCHING BIRDS (a bird that touches the slingshot joins the waiting line)
    //  - Fast route: normal collision callbacks.
    //  - Main route: every 0.1s, check every bird's distance to the slingshot.
    // ------------------------------------------------------------
    void OnCollisionEnter2D(Collision2D c)
    {
        TryAttachFrom(c.collider);
    }
 
    void OnCollisionStay2D(Collision2D c)
    {
        TryAttachFrom(c.collider);
    }
 
    void TryAttachFrom(Collider2D col)
    {
        if (col == null || col.isTrigger)
            return;
        Rigidbody2D rb = col.attachedRigidbody;
        AngryBird b = rb != null ? rb.GetComponent<AngryBird>() : col.GetComponent<AngryBird>();
        if (b != null)
            TryAttach(b);
    }
 
    void FixedUpdate()
    {
        scanTimer -= Time.fixedDeltaTime;
        if (scanTimer > 0f)
            return;
        scanTimer = 0.1f;
 
        Collider2D body = GetComponent<Collider2D>();
        AngryBird[] birds = FindObjectsOfType<AngryBird>();
        foreach (AngryBird b in birds)
        {
            if (b == null || IsTracked(b))
                continue;
            if (DistanceToBody(b, body) < catchDistance || SpritesOverlap(b))
                TryAttach(b);
        }
    }
 
    float DistanceToBody(AngryBird b, Collider2D body)
    {
        float best = float.MaxValue;
        bool solid = false;
        if (body != null)
        {
            foreach (Collider2D c in b.GetComponents<Collider2D>())
            {
                if (c == null || c.isTrigger || !c.enabled)
                    continue;
                solid = true;
                ColliderDistance2D d = body.Distance(c);
                if (d.isValid)
                    best = Mathf.Min(best, d.isOverlapped ? 0f : d.distance);
            }
        }
        if (!solid)
        {
            // No solid collider to measure with: fall back to the bird's position vs. the slingshot's bounds
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (body == null && sr == null)
                return float.MaxValue;
            Bounds bnd = body != null ? body.bounds : sr.bounds;
            best = Mathf.Sqrt(bnd.SqrDistance(b.transform.position));
        }
        return best;
    }
 
    // Physics-free check: do the bird's picture and the slingshot's picture overlap?
    bool SpritesOverlap(AngryBird b)
    {
        SpriteRenderer mine = GetComponent<SpriteRenderer>();
        SpriteRenderer theirs = b.GetComponent<SpriteRenderer>();
        if (mine == null || theirs == null || !theirs.enabled)
            return false;
        return Flat(mine.bounds, 0.85f).Intersects(Flat(theirs.bounds, 0.6f));
    }
 
    // The same box, shrunk a little and flattened in depth so different z positions can't matter
    static Bounds Flat(Bounds bnd, float shrink)
    {
        return new Bounds(new Vector3(bnd.center.x, bnd.center.y, 0f), new Vector3(bnd.size.x * shrink, bnd.size.y * shrink, 2f));
    }
 
    bool IsTracked(AngryBird b)
    {
        if (cup != null && cup.bird == b)
            return true;
        return waiting.Contains(b);
    }
 
    void TryAttach(AngryBird b)
    {
        if (b == null || IsTracked(b))
            return;
        int total = waiting.Count + (cup != null ? 1 : 0);
        if (total >= maxQueued + 1)
            return;
 
        float until = 0f;
        if (blockedUntil.TryGetValue(b, out until) && Time.time < until)
            return;
 
        Rigidbody2D rb = b.GetComponent<Rigidbody2D>();
        if (rb == null || !rb.simulated)         // already frozen by something else (e.g. Telebird mid-teleport)
            return;
 
        // Just put it in line. It is NOT frozen: it keeps standing/rolling on the ground until it's its turn.
        waiting.Add(b);
        b.shotFromSlingshot = false;             // a caught bird is no longer "shot" (no idle despawn while it waits)
        b.hasCollidedAfterShot = false;
        BirdTrail oldTrail = b.GetComponent<BirdTrail>();
        if (oldTrail != null)
            oldTrail.Clear();
 
        // Draw the slingshot behind the birds
        SpriteRenderer body = GetComponent<SpriteRenderer>();
        SpriteRenderer birdSr = b.GetComponent<SpriteRenderer>();
        if (body != null && birdSr != null && body.sortingOrder >= birdSr.sortingOrder)
            body.sortingOrder = birdSr.sortingOrder - 1;
    }
 
    // The waiting bird that is closest to the cup, that can actually be picked up right now
    AngryBird PickNextBird()
    {
        Vector2 cupWorld = (Vector2)transform.TransformPoint(cupOffset);
        AngryBird best = null;
        float bestDist = float.MaxValue;
        foreach (AngryBird b in waiting)
        {
            if (b == null)
                continue;
            Rigidbody2D rb = b.GetComponent<Rigidbody2D>();
            if (rb == null || !rb.simulated)     // frozen by something else right now: try again later
                continue;
            float d = Utility.Distance((Vector2)b.transform.position, cupWorld);
            if (d < bestDist)
            {
                bestDist = d;
                best = b;
            }
        }
        return best;
    }
 
    // A waiting bird is picked: freeze it where it stands and let it hop into the cup from there
    void MoveToCup(AngryBird b)
    {
        waiting.Remove(b);
 
        Rigidbody2D rb = b.GetComponent<Rigidbody2D>();
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;
        b.stat = AngryBird.Status.Idle;
 
        Rec r = new Rec();
        r.bird = b;
        r.cur = (Vector2)transform.InverseTransformPoint(b.transform.position);
        r.from = r.cur;
        r.target = r.cur;
        r.t = 1f;
        cup = r;
    }
 
    // ------------------------------------------------------------
    // PLACING AND HOPPING THE CUP BIRD (or holding it where you've pulled it)
    // ------------------------------------------------------------
    void LateUpdate()
    {
        if (cup != null && cup.bird == null)
        {
            cup = null;
            if (pulling)
                EndPull();
        }
 
        for (int i = waiting.Count - 1; i >= 0; i--)
        {
            AngryBird w = waiting[i];
            if (w == null)
            {
                waiting.RemoveAt(i);
                continue;
            }
            // knocked or wandered too far away: it's no longer waiting (it can be caught again by touching)
            if (Utility.Distance((Vector2)w.transform.position, (Vector2)transform.position) > maxWaitDistance)
                waiting.RemoveAt(i);
        }
 
        if (cup == null)
            return;
 
        if (pulling)
        {
            // The bird is held where you've pulled it
            Vector2 anchor = (Vector2)transform.TransformPoint(cupOffset);
            Vector2 p = anchor + pull;
            cup.bird.transform.position = new Vector3(p.x, p.y, cup.bird.transform.position.z);
            if (rotateWhilePulling && pull.sqrMagnitude > 0.01f)
            {
                float a = Mathf.Atan2(-pull.y, -pull.x) * Mathf.Rad2Deg;
                cup.bird.transform.rotation = Quaternion.Euler(0f, 0f, a);
            }
            else
                cup.bird.transform.rotation = transform.rotation;
            cup.cur = (Vector2)transform.InverseTransformPoint(cup.bird.transform.position);
        }
        else
            MoveRec(cup, cupOffset);
    }
 
    // Moves the bird toward its slot; whenever its slot changes it hops there in an arc
    void MoveRec(Rec r, Vector2 target)
    {
        if (r.target != target)
        {
            r.from = r.cur;
            r.target = target;
            r.t = 0f;
            // sometimes he spins on the way in
            r.spinTurns = UnityEngine.Random.value < hopSpinChance ? (UnityEngine.Random.value < 0.5f ? -1f : 1f) : 0f;
        }
        if (r.t < 1f)
            r.t = Mathf.Min(1f, r.t + Time.deltaTime / Mathf.Max(0.01f, hopTime));
 
        float arc = 4f * r.t * (1f - r.t) * hopHeight;      // 0 -> peak -> 0
        r.cur = Vector2.Lerp(r.from, r.target, r.t);
        Vector3 world = transform.TransformPoint(r.cur + Vector2.up * arc);
        r.bird.transform.position = new Vector3(world.x, world.y, r.bird.transform.position.z);
        float z = transform.eulerAngles.z + r.spinTurns * 360f * r.t;       // ends exactly upright, whatever the spin
        r.bird.transform.rotation = Quaternion.Euler(0f, 0f, z);
    }
 
    // ------------------------------------------------------------
    // PULLING BACK WITH THE MOUSE
    // ------------------------------------------------------------
    Vector2 MouseWorld()
    {
        if (Camera.main == null)
            return (Vector2)transform.position;
        return (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }
 
    void HandlePull()
    {
        bool ready = cup != null && cup.bird != null && cup.t >= 1f;
        Vector2 anchor = (Vector2)transform.TransformPoint(cupOffset);
        Vector2 mouse = MouseWorld();
 
        if (!pulling)
        {
            // Click on the ready bird to grab it
            if (ready && Input.GetMouseButtonDown(0) && Vector2.Distance(mouse, anchor) <= grabRadius)
                BeginPull();
            return;
        }
 
        if (!ready)
        {
            EndPull();
            return;
        }
 
        // Follow the mouse, but only up to maxPull away from the cup
        Vector2 p = mouse - anchor;
        if (p.magnitude > maxPull)
            p = p.normalized * maxPull;
        pull = p;
 
        // Let go: fire (or cancel if barely pulled)
        if (Input.GetMouseButtonUp(0) || !Input.GetMouseButton(0))
        {
            if (pull.magnitude >= minPull)
                FireFromPull();
            else
                CancelPull();
        }
    }
 
    void BeginPull()
    {
        pulling = true;
        pull = Vector2.zero;
 
        // Stop the game's own mouse-dragging from yanking the slingshot around while you pull
        Rigidbody2D srb = GetComponent<Rigidbody2D>();
        if (srb != null)
        {
            savedBodyType = srb.bodyType;
            srb.velocity = Vector2.zero;
            srb.angularVelocity = 0f;
            srb.bodyType = RigidbodyType2D.Kinematic;
        }
 
        // Sounds: the stretch loop, and the bird's own stretch voice once
        if (slingSound != null && stretchClip != null)
        {
            slingSound.clip = stretchClip;
            slingSound.volume = stretchVolume;
            slingSound.loop = true;
            slingSound.Play();
        }
        PlayStretchVoice(cup.bird);
    }
 
    // The bird's "_voice_stretch" / "_stretch_2" / "_stretch_3" clip (whichever files exist), once
    void PlayStretchVoice(AngryBird b)
    {
        if (b == null || b.mouth == null || b.stretch == null)
            return;
        List<AudioClip> found = new List<AudioClip>();
        for (int i = 1; i <= 3; i++)
            if (b.stretch[i] != null)
                found.Add(b.stretch[i]);
        if (found.Count == 0)
            return;
        b.mouth.clip = found[UnityEngine.Random.Range(0, found.Count)];
        b.mouth.Play();
    }
 
    // Stops the pulling state (sound, slingshot body); the bird itself is handled by the caller
    void EndPull()
    {
        pulling = false;
        if (slingSound != null)
            slingSound.Stop();
        Rigidbody2D srb = GetComponent<Rigidbody2D>();
        if (srb != null && srb.bodyType == RigidbodyType2D.Kinematic)
            srb.bodyType = savedBodyType;
    }
 
    // Let go without pulling far enough: the bird hops back into the cup
    void CancelPull()
    {
        if (cup != null && cup.bird != null)
        {
            if (cup.bird.mouth != null)
                cup.bird.mouth.Stop();
            cup.from = cup.cur;     // hop back from where it's being held
            cup.t = 0f;
            cup.spinTurns = 0f;
        }
        EndPull();
    }
 
    void FireFromPull()
    {
        Vector2 dir = (-pull).normalized;                               // away from where you pulled
        float fraction = Mathf.Clamp01(pull.magnitude / maxPull);
        float speed = launchSpeed * Mathf.Max(minSpeedFraction, fraction);
 
        EndPull();
        if (slingSound != null && fireClip != null)
        {
            slingSound.volume = fireVolume;
            slingSound.PlayOneShot(fireClip);
        }
        LaunchCup(dir, speed);
    }
 
    // The fixed launch direction (only used when activateLaunches is on)
    Vector2 FixedDirection()
    {
        float a = launchAngle * Mathf.Deg2Rad;
        return ((Vector2)transform.TransformVector(new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f))).normalized;
    }
 
    // A dotted preview of the flight path while pulling (ignores obstacles)
    void UpdateGuide()
    {
        bool show = showGuide && pulling && cup != null && cup.bird != null && pull.magnitude >= minPull;
 
        int wanted = Mathf.Max(1, Mathf.RoundToInt(guideDots * guideLengthMultiplier));
 
        // Build the dots, or rebuild them if the length or colour changed (e.g. a Sling Scope was just added)
        if (dots != null && (dots.Length != wanted || dotsBuiltColor != guideColor))
        {
            foreach (GameObject old in dots)
                if (old != null)
                    Destroy(old);
            dots = null;
        }
 
        if (dots == null)
        {
            if (!show)
                return;
            dots = new GameObject[wanted];
            dotsBuiltColor = guideColor;
            Sprite dot = GetDotSprite();
            for (int i = 0; i < wanted; i++)
            {
                float f = wanted > 1 ? (float)i / (wanted - 1) : 0f;   // 0 at the slingshot, 1 at the far end
                GameObject g = new GameObject("AimDot");
                SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
                sr.sprite = dot;
                sr.sortingOrder = 60;
                Color c = guideColor;
                c.a = Mathf.Lerp(0.9f, 0.55f, f);                      // same fade as before, spread over the whole length
                sr.color = c;
                g.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.72f, f);
                dots[i] = g;
            }
        }
 
        if (!show)
        {
            foreach (GameObject g in dots)
                if (g != null)
                    g.SetActive(false);
            return;
        }
 
        Vector2 anchor = (Vector2)transform.TransformPoint(cupOffset);
        Vector2 p0 = anchor + pull;                                     // where the bird is being held
        float fraction = Mathf.Clamp01(pull.magnitude / maxPull);
        Vector2 v = (-pull).normalized * (launchSpeed * Mathf.Max(minSpeedFraction, fraction));
        Rigidbody2D rb = cup.bird.GetComponent<Rigidbody2D>();
        Vector2 gravity = Physics2D.gravity * (rb != null ? rb.gravityScale : 1f);
        SpaceFlight spaceFlight = cup.bird.GetComponent<SpaceFlight>();
        if (spaceFlight != null && spaceFlight.zeroGravityOnLaunch)
            gravity = Vector2.zero;                // Space birds fly straight, so the guide is a straight line
 
        for (int i = 0; i < dots.Length; i++)
        {
            if (dots[i] == null)
                continue;
            float t = (i + 1) * guideStep;
            Vector2 p = p0 + v * t + 0.5f * gravity * t * t;
            dots[i].SetActive(true);
            dots[i].transform.position = new Vector3(p.x, p.y, transform.position.z);
        }
    }
 
    // A small round dot, generated in code so no extra image is needed
    static Sprite GetDotSprite()
    {
        if (dotSprite != null)
            return dotSprite;
        int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float c = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                float alpha = Mathf.Clamp01(size / 2f - dist);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        dotSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return dotSprite;
    }
 
    // ------------------------------------------------------------
    // LAUNCHING
    // ------------------------------------------------------------
    void LaunchCup(Vector2 dir, float speed)
    {
        // Only fire a bird that has finished hopping into the cup
        if (cup == null || cup.bird == null || cup.t < 1f)
            return;
 
        AngryBird b = cup.bird;
        Rigidbody2D rb = b.GetComponent<Rigidbody2D>();
        if (rb == null)
            return;
 
        if (pulling)
            EndPull();
 
        cup = null;
        reloadTimer = reloadDelay;          // the next bird hops on after this long
 
        StartCoroutine(IgnoreBriefly(b, 0.5f));   // don't bump into the slingshot on the way out
        blockedUntil[b] = Time.time + 1f;
 
        rb.simulated = true;
        if (b.gameMass > 0f)
            rb.mass = b.gameMass;                 // the heavier "in flight" mass every bird normally has
        rb.velocity = dir * speed;
        rb.angularVelocity = 0f;
 
        b.stat = AngryBird.Status.SlingFly;
        b.shotFromSlingshot = true;
        b.hasCollidedAfterShot = false;       // a fresh shot unlocks the ability again
        BirdTrail.Begin(b);
        b.Say(b.launch);
        if (b.isTerence)
        {
            Terence t = b.GetComponent<Terence>();
            if (t != null)
                t.firTimeToStep = true;
        }
 
        lastShot = b;
        lastShotUsed = false;
 
        if (!hinted)
        {
            hinted = true;
            ModAPI.Notify("<color=green>Press X to activate the last shot bird's power.</color>");
        }
    }
 
    IEnumerator IgnoreBriefly(AngryBird b, float seconds)
    {
        Collider2D[] mine = GetComponents<Collider2D>();
        Collider2D[] theirs = b.GetComponents<Collider2D>();
        foreach (Collider2D m in mine)
            foreach (Collider2D t in theirs)
                if (m != null && t != null)
                    Physics2D.IgnoreCollision(m, t, true);
 
        yield return new WaitForSeconds(seconds);
 
        foreach (Collider2D m in mine)
            foreach (Collider2D t in theirs)
                if (m != null && t != null)
                    Physics2D.IgnoreCollision(m, t, false);
    }
 
    // ------------------------------------------------------------
    // RELOADING + PULLING + X
    // ------------------------------------------------------------
    void Update()
    {
        // Reload: once the delay has passed, the closest waiting bird hops into the empty cup
        if (reloadTimer > 0f)
            reloadTimer -= Time.deltaTime;
        if (cup == null && reloadTimer <= 0f && waiting.Count > 0)
        {
            AngryBird next = PickNextBird();
            if (next != null)
                MoveToCup(next);
        }
 
        UpdateIdleTricks();
        HandlePull();
        UpdateGuide();
 
        // X = use the power of the last shot bird
        if (lastShot != null && !lastShotUsed && InputSystem.Down("Slingshot X: activate birds"))
        {
            UseEventTrigger trigger = lastShot.GetComponent<UseEventTrigger>();
            if (trigger != null && trigger.Action != null)
                trigger.Action();
            if (activateOnce)
                lastShotUsed = true;
        }
    }
 
    // If the slingshot is deleted, don't leave the cup bird frozen, and clean up the guide
    void OnDestroy()
    {
        if (slingSound != null)
            slingSound.Stop();
        if (cup != null && cup.bird != null)
        {
            Rigidbody2D rb = cup.bird.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.simulated = true;
        }
 
        if (dots != null)
        {
            foreach (GameObject g in dots)
                if (g != null)
                    Destroy(g);
        }
    }
}
// The Kingsling item: touches a Slingshot X -> upgrades it, then disappears
public class Kingsling : MonoBehaviour
{
    public float speedMultiplier = 1.7f;                                  // launch speed after the upgrade (1.5 = 50% faster)
    public Color32 glowColor = new Color32(255, 220, 40, 255);            // the slingshot's glow
    public float soundVolume = 4f;                                        // how long (seconds) the sound is allowed to play
 
    bool used = false;
    float scanTimer = 0f;
    float msgCooldown = 0f;
 
    void OnCollisionEnter2D(Collision2D c)
    {
        TryUpgrade(c.collider);
    }
 
    void OnCollisionStay2D(Collision2D c)
    {
        TryUpgrade(c.collider);
    }
 
    void TryUpgrade(Collider2D col)
    {
        if (col == null || used)
            return;
        Rigidbody2D rb = col.attachedRigidbody;
        SlingshotX s = rb != null ? rb.GetComponent<SlingshotX>() : col.GetComponent<SlingshotX>();
        if (s != null)
            Upgrade(s);
    }
 
    // Backup for when the collision callbacks don't fire: every 0.1s check if we're touching any Slingshot X
    void FixedUpdate()
    {
        if (used)
            return;
        scanTimer -= Time.fixedDeltaTime;
        if (scanTimer > 0f)
            return;
        scanTimer = 0.1f;
 
        Collider2D mine = GetComponent<Collider2D>();
        if (mine == null)
            return;
        SlingshotX[] slings = FindObjectsOfType<SlingshotX>();
        foreach (SlingshotX s in slings)
        {
            if (s == null)
                continue;
            foreach (Collider2D c in s.GetComponents<Collider2D>())
            {
                if (c == null || c.isTrigger || !c.enabled)
                    continue;
                ColliderDistance2D d = mine.Distance(c);
                if (d.isValid && (d.isOverlapped || d.distance < 0.1f))
                {
                    Upgrade(s);
                    return;
                }
            }
        }
    }
 
    void Update()
    {
        if (msgCooldown > 0f)
            msgCooldown -= Time.deltaTime;
    }
 
    void Upgrade(SlingshotX s)
    {
        if (used)
            return;
 
        SlingshotPowerUps p = s.GetComponent<SlingshotPowerUps>();
        if (p == null)
            p = s.gameObject.AddComponent<SlingshotPowerUps>();
 
        // Same power up twice doesn't stack: the item stays where it is
        if (p.Has("Kingsling"))
        {
            if (msgCooldown <= 0f)
            {
                ModAPI.Notify("<color=yellow>This slingshot already has a Kingsling.</color>");
                msgCooldown = 3f;
            }
            return;
        }
 
        used = true;
        p.Add("Kingsling", glowColor);
        s.launchSpeed *= speedMultiplier;
 
        AudioClip clip = ModAPI2.LoadSound("AudioClip/kingsling.wav");
        if (clip != null)
            Utility.CreateOneShotAudio(clip, s.transform.position, soundVolume);
        else
            ModAPI.Notify("<color=red>kingsling.wav didn't load</color>");
        ModAPI.CreateParticleEffect("Flash", s.transform.position);
        ModAPI.Notify("<color=yellow>Kingsling! This slingshot launches birds faster now.</color>");
 
        Destroy(gameObject);
    }
}
// Base for power up items: touch a Slingshot X -> it's upgraded, plays kingsling.wav, the item is used up.
// To make a new power up, derive from this and fill in the three things below.
public abstract class PowerUpItem : MonoBehaviour
{
    protected abstract string PowerUpName { get; }                   // used to stop the same power up stacking on itself
    protected abstract Color32 GlowColor { get; }                    // the slingshot's glow
    protected abstract void Apply(SlingshotX s);                     // what the upgrade actually does
 
    // Optional extras (the Kingsling and the Sling Scope don't need them)
    protected virtual bool IsPermanent { get { return true; } }      // true = upgrades the slingshot (glow, can't be added twice); false = a one-off
    protected virtual bool CanApply(SlingshotX s) { return true; }   // return false to refuse (use Say() to tell the player why)
    protected virtual string SuccessMessage(SlingshotX s) { return PowerUpName + " added to the slingshot!"; }
 
    public float soundVolume = 4f;                                   // how long (seconds) the sound is allowed to play
 
    bool used = false;
    float scanTimer = 0f;
    float msgCooldown = 0f;
 
    // A message that can't spam (at most one every 3 seconds)
    protected void Say(string msg)
    {
        if (msgCooldown > 0f)
            return;
        ModAPI.Notify(msg);
        msgCooldown = 3f;
    }
 
    void OnCollisionEnter2D(Collision2D c)
    {
        TryUpgrade(c.collider);
    }
 
    void OnCollisionStay2D(Collision2D c)
    {
        TryUpgrade(c.collider);
    }
 
    void TryUpgrade(Collider2D col)
    {
        if (col == null || used)
            return;
        Rigidbody2D rb = col.attachedRigidbody;
        SlingshotX s = rb != null ? rb.GetComponent<SlingshotX>() : col.GetComponent<SlingshotX>();
        if (s != null)
            Upgrade(s);
    }
 
    // Backup for when the collision callbacks don't fire: every 0.1s check if we're touching any Slingshot X
    void FixedUpdate()
    {
        if (used)
            return;
        scanTimer -= Time.fixedDeltaTime;
        if (scanTimer > 0f)
            return;
        scanTimer = 0.1f;
 
        Collider2D mine = GetComponent<Collider2D>();
        if (mine == null)
            return;
        SlingshotX[] slings = FindObjectsOfType<SlingshotX>();
        foreach (SlingshotX s in slings)
        {
            if (s == null)
                continue;
            foreach (Collider2D c in s.GetComponents<Collider2D>())
            {
                if (c == null || c.isTrigger || !c.enabled)
                    continue;
                ColliderDistance2D d = mine.Distance(c);
                if (d.isValid && (d.isOverlapped || d.distance < 0.1f))
                {
                    Upgrade(s);
                    return;
                }
            }
        }
    }
 
    void Update()
    {
        if (msgCooldown > 0f)
            msgCooldown -= Time.deltaTime;
    }
 
    void Upgrade(SlingshotX s)
    {
        if (used)
            return;
 
        SlingshotPowerUps p = null;
        if (IsPermanent)
        {
            p = s.GetComponent<SlingshotPowerUps>();
            if (p == null)
                p = s.gameObject.AddComponent<SlingshotPowerUps>();
 
            // The same power up twice doesn't stack: the item stays where it is
            if (p.Has(PowerUpName))
            {
                Say("<color=yellow>This slingshot already has a " + PowerUpName + ".</color>");
                return;
            }
        }
 
        if (!CanApply(s))
            return;
 
        used = true;
        if (IsPermanent)
            p.Add(PowerUpName, GlowColor);
        Apply(s);
 
        AudioClip clip = ModAPI2.LoadSound("AudioClip/kingsling.wav");
        if (clip != null)
            Utility.CreateOneShotAudio(clip, s.transform.position, soundVolume);
        else
            ModAPI.Notify("<color=red>kingsling.wav didn't load</color>");
        ModAPI.CreateParticleEffect("Flash", s.transform.position);
        ModAPI.Notify("<color=yellow>" + SuccessMessage(s) + "</color>");
 
        Destroy(gameObject);
    }
}
 
// The Sling Scope item: a much longer, red aiming guide
public class SlingScope : PowerUpItem
{
    public float lengthMultiplier = 14f;                                   // how many times longer the guide gets
    public Color guideDotColor = new Color(1f, 0.80f, 0.80f, 1f);          // red dots
    public Color32 glowColor = new Color32(255, 20, 20, 255);              // red glow
 
    protected override string PowerUpName { get { return "Sling Scope"; } }
    protected override Color32 GlowColor { get { return glowColor; } }
 
    protected override void Apply(SlingshotX s)
    {
        s.guideLengthMultiplier = lengthMultiplier;
        s.guideColor = guideDotColor;
    }
}

// The Power Potion item: the bird on the slingshot drinks it and gets bigger and heavier
public class PowerPotion : PowerUpItem
{
    public float sizeMultiplier = 1.4f;     // "slightly bigger"
    public float massMultiplier = 2f;       // how many times heavier
 
    protected override string PowerUpName { get { return "Power Potion"; } }
    protected override Color32 GlowColor { get { return new Color32(190, 90, 255, 255); } }   // (not used: the potion gives the slingshot no glow)
    protected override bool IsPermanent { get { return false; } }
 
    protected override bool CanApply(SlingshotX s)
    {
        AngryBird b = s.CupBird;
        if (b == null)
        {
            Say("<color=yellow>There's no bird on the slingshot to drink the Power Potion.</color>");
            return false;
        }
        if (b.GetComponent<PowerPotionMark>() != null)
        {
            Say("<color=yellow>This bird already drank a Power Potion.</color>");
            return false;
        }
        return true;
    }
 
    protected override void Apply(SlingshotX s)
    {
        AngryBird b = s.CupBird;
        PowerPotionMark m = b.gameObject.AddComponent<PowerPotionMark>();
        m.Boost(sizeMultiplier, massMultiplier);
        ModAPI.CreateParticleEffect("Flash", b.transform.position);
    }
 
    protected override string SuccessMessage(SlingshotX s)
    {
        return "The bird drank the Power Potion!";
    }
}
 
// Put on a bird that drank a Power Potion (also stops it drinking a second one)
public class PowerPotionMark : MonoBehaviour
{
    public float growTime = 0.4f;     // seconds the bird takes to grow to its new size
 
    public void Boost(float sizeMult, float massMult)
    {
        // Mass: the normal mass, the heavier "in flight" mass the slingshot uses, and what it weighs right now
        AngryBird b = GetComponent<AngryBird>();
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (b != null)
        {
            b.mass *= massMult;
            b.gameMass *= massMult;
        }
        if (rb != null)
            rb.mass *= massMult;
 
        StartCoroutine(Grow(sizeMult));
    }
 
    IEnumerator Grow(float mult)
    {
        Vector3 start = transform.localScale;
        float ax = Mathf.Abs(start.x);
        float ay = Mathf.Abs(start.y);
 
        float t = 0f;
        while (t < growTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Lerp(1f, mult, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / growTime)));
            Vector3 s = transform.localScale;      // keep whichever way he's facing (the sign of the scale)
            transform.localScale = new Vector3(Mathf.Sign(s.x) * ax * k, Mathf.Sign(s.y) * ay * k, s.z);
            yield return null;
        }
    }
}
// The Birdquake item: activate it to start the earthquake
public class Birdquake : MonoBehaviour
{
    public bool consumeItem = true;       // true = the item is used up; false = it stays and can be used again once the quake is over
    public float soundVolume = 6f;        // how long (seconds) the sound is allowed to play
 
    bool busy = false;
 
    void Start()
    {
        if (!gameObject.GetComponent<TriggerEventBehaviour>())
        {
            gameObject.AddComponent<UseEventTrigger>().Action = () =>
            {
                if (busy)
                    return;
                StartQuake();
            };
        }
    }
 
    void StartQuake()
    {
        busy = true;
 
        GameObject q = new GameObject("Birdquake");
        BirdquakeRunner r = q.AddComponent<BirdquakeRunner>();
        r.origin = transform.position;
 
        AudioClip clip = ModAPI2.LoadSound("AudioClip/birdquake.wav");
        if (clip == null || clip.length < 0.01f)
            clip = ModAPI2.LoadSound("AudioClip/kingsling.wav");
        if (clip != null)
            Utility.CreateOneShotAudio(clip, transform.position, soundVolume);
 
        if (consumeItem)
        {
            Destroy(gameObject);
        }
        else
        {
            r.onFinished = () => { busy = false; };
        }
    }
}
 
// The earthquake itself: shakes the screen and jostles every moving object for a few seconds
public class BirdquakeRunner : MonoBehaviour
{
    // Tuning knobs
    public float duration = 4f;           // total length of the quake (seconds)
    public float rampUp = 0.8f;           // seconds it takes to build up
    public float rampDown = 1.8f;         // seconds it takes to die away at the end
    public float pulseInterval = 0.2f;   // how often everything gets jostled (seconds)
    public float sideSpeed = 1f;        // sideways shove per jostle (random left/right, up to this much)
    public float hopSpeed = 1f;           // little upward hop per jostle (only for things resting on the ground)
    public float spinSpeed = 20f;         // random spin per jostle (degrees per second)
    public float shakeStrength = 240f;    // screen shake (Terence uses 256; this is a "minor" one)
 
    [System.NonSerialized]
    public Vector3 origin;                // (just used as a fallback place for the screen shake)
    [System.NonSerialized]
    public System.Action onFinished = null;
 
    float t = 0f;
    float pulseTimer = 0f;
 
    // 0..1: builds up, holds, then fades out
    float Envelope()
    {
        float e = 1f;
        if (t < rampUp)
            e = t / rampUp;
        float left = duration - t;
        if (left < rampDown)
            e = Mathf.Min(e, left / rampDown);
        return Mathf.Clamp01(e);
    }
 
    void Update()
    {
        t += Time.deltaTime;
 
        // The screen rumbles (aimed at the camera so it shakes fully wherever the item was)
        if (CameraShakeBehaviour.main != null)
        {
            Vector3 camPos = Camera.main != null ? Camera.main.transform.position : origin;
            CameraShakeBehaviour.main.Shake(shakeStrength * Envelope(), camPos);
        }
 
        if (t >= duration)
        {
            if (onFinished != null)
                onFinished();
            Destroy(gameObject);
        }
    }
 
    void FixedUpdate()
    {
        pulseTimer -= Time.fixedDeltaTime;
        if (pulseTimer > 0f)
            return;
        pulseTimer = pulseInterval;
 
        float env = Envelope();
        if (env <= 0f)
            return;
 
        Rigidbody2D[] all = FindObjectsOfType<Rigidbody2D>();
        foreach (Rigidbody2D rb in all)
        {
            // Only things that can actually move (the ground, map blocks and frozen things stay put)
            if (rb == null || !rb.simulated || rb.bodyType != RigidbodyType2D.Dynamic)
                continue;
 
            Vector2 v = rb.velocity;
            v.x += UnityEngine.Random.Range(-1f, 1f) * sideSpeed * env;
            if (Mathf.Abs(v.y) < 1.5f)       // a hop only for things that aren't already flying or falling
                v.y += UnityEngine.Random.Range(0f, 1f) * hopSpeed * env;
            rb.velocity = v;
            rb.angularVelocity += UnityEngine.Random.Range(-1f, 1f) * spinSpeed * env;
            rb.WakeUp();
        }
    }
}
// Sits on a Slingshot X and remembers which power ups it has; draws one soft pulsing glow per power up
public class SlingshotPowerUps : MonoBehaviour
{
    class Glow
    {
        public GameObject go;
        public SpriteRenderer sr;
        public Color color;
        public int index;
    }
 
    public float glowSizeFactor = 1.8f;    // glow size compared to the slingshot's picture
    public float glowStep = 0.35f;         // extra size for each additional power up, so stacked glows show side by side
    public float pulseSpeed = 3f;
    public float minAlpha = 0.35f;
    public float maxAlpha = 0.75f;
 
    Dictionary<string, Glow> glows = new Dictionary<string, Glow>();
    SpriteRenderer body;
    static Sprite glowSprite = null;
 
    void Awake()
    {
        body = GetComponent<SpriteRenderer>();
    }
 
    public bool Has(string powerUp)
    {
        return glows.ContainsKey(powerUp);
    }
 
    // Returns false (and does nothing) if the slingshot already has it
    public bool Add(string powerUp, Color32 color)
    {
        if (glows.ContainsKey(powerUp))
            return false;
 
        Glow g = new Glow();
        g.go = new GameObject("PowerUpGlow_" + powerUp);
        g.sr = g.go.AddComponent<SpriteRenderer>();
        g.sr.sprite = GetGlowSprite();
        g.color = color;
        g.index = glows.Count;
        if (body != null)
        {
            g.sr.sortingLayerID = body.sortingLayerID;
            g.sr.sortingOrder = body.sortingOrder - 1 - g.index;   // behind the slingshot
        }
        glows[powerUp] = g;
        return true;
    }
 
    void LateUpdate()
    {
        if (body == null)
            return;
        Bounds b = body.bounds;
        float baseSize = Mathf.Max(b.size.x, b.size.y);
        foreach (Glow g in glows.Values)
        {
            if (g.go == null)
                continue;
            float size = baseSize * glowSizeFactor + g.index * glowStep;
            g.go.transform.position = new Vector3(b.center.x, b.center.y, transform.position.z);
            g.go.transform.rotation = Quaternion.identity;
            g.go.transform.localScale = new Vector3(size, size, 1f);
 
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed + g.index);
            Color c = g.color;
            c.a = Mathf.Lerp(minAlpha, maxAlpha, pulse);
            g.sr.color = c;
        }
    }
 
    void OnDestroy()
    {
        foreach (Glow g in glows.Values)
            if (g.go != null)
                Destroy(g.go);
    }
 
    // A soft round blob (white, fades toward the edge), generated in code so no extra image is needed
    static Sprite GetGlowSprite()
    {
        if (glowSprite != null)
            return glowSprite;
        int n = 64;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float r = n * 0.5f;
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x + 0.5f - r) * (x + 0.5f - r) + (y + 0.5f - r) * (y + 0.5f - r)) / r;
                float a = Mathf.Clamp01(1f - d);
                a = a * a;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        glowSprite = Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), n);   // 1 world unit wide
        return glowSprite;
    }
}
public class BirdTrail : MonoBehaviour
{
    // Knobs
    public float dotSpacing = 0.7f;       // distance between trail dots
    public float dotScale = 0.8f;         // dot size
    public Color dotColor = new Color(1f, 1f, 1f, 0.7f);
    public float cloudScale = 1.2f;       // size of the ability cloud
    public Color cloudColor = new Color(1f, 1f, 1f, 0.8f);
    public int maxDots = 4000;             // oldest dots are removed past this
 
    AngryBird bird;
    GameObject holder;                    // every dot and cloud lives under this, so one Destroy removes the whole trail
    List<GameObject> dots = new List<GameObject>();
    List<Transform> clouds = new List<Transform>();
    List<float> cloudBorn = new List<float>();
    bool tracking = false;
    Vector2 lastPos;
    float lastMarkTime = -10f;
 
    static Sprite dotSprite = null;
    static Sprite cloudSprite = null;
 
    // Called by SlingshotX when a bird is launched: starts a fresh trail
    public static void Begin(AngryBird b)
    {
        BirdTrail t = b.GetComponent<BirdTrail>();
        if (t == null)
            t = b.gameObject.AddComponent<BirdTrail>();
        t.Restart();
    }
 
    void Restart()
    {
        bird = GetComponent<AngryBird>();
        Clear();
        holder = new GameObject("BirdTrail");
        tracking = true;
        lastPos = (Vector2)transform.position;
    }
 
    // Removes the whole trail
    public void Clear()
    {
        if (holder != null)
            Destroy(holder);
        holder = null;
        dots.Clear();
        clouds.Clear();
        cloudBorn.Clear();
        tracking = false;
    }
 
    void OnDestroy()
    {
        Clear();
    }
 
    void Update()
    {
        if (holder == null)
            return;
 
        // The bird died / vanished (Vanish turns the AngryBird script off): the trail goes with it
        if (bird == null || !bird.enabled)
        {
            Clear();
            return;
        }
 
        // Leave a dot every dotSpacing along the flight, until the first thing it hits
        if (tracking)
        {
            if (bird.hasCollidedAfterShot)
            {
                tracking = false;
            }
            else
            {
                Vector2 pos = (Vector2)transform.position;
                float d = Vector2.Distance(pos, lastPos);
                if (d > 12f)
                {
                    lastPos = pos;          // teleported: no dotted line across the gap
                }
                else
                {
                    while (d >= dotSpacing)
                    {
                        lastPos = Vector2.MoveTowards(lastPos, pos, dotSpacing);
                        AddDot(lastPos);
                        d = Vector2.Distance(pos, lastPos);
                    }
                }
            }
        }
 
        // Ability clouds pop up to full size
        for (int i = 0; i < clouds.Count; i++)
        {
            if (clouds[i] == null)
                continue;
            float k = Mathf.Clamp01((Time.time - cloudBorn[i]) / 0.2f);
            clouds[i].localScale = Vector3.one * cloudScale * (0.3f + 0.7f * k);
        }
    }
 
    // Marks where the bird's ability went off (called when the ability is used)
    public void MarkAbility()
    {
        if (holder == null)
            return;
        if (Time.time - lastMarkTime < 0.3f)      // several code paths can report the same activation
            return;
        lastMarkTime = Time.time;
 
        GameObject g = NewSprite("AbilityCloud", GetCloudSprite(), cloudColor, (Vector2)transform.position, 1);
        g.transform.localScale = Vector3.one * cloudScale * 0.3f;
        clouds.Add(g.transform);
        cloudBorn.Add(Time.time);
    }
 
    void AddDot(Vector2 p)
    {
        GameObject g = NewSprite("TrailDot", GetDotSprite(), dotColor, p, 0);
        g.transform.localScale = Vector3.one * dotScale;
        dots.Add(g);
        if (dots.Count > maxDots)
        {
            Destroy(dots[0]);
            dots.RemoveAt(0);
        }
    }
 
    GameObject NewSprite(string name, Sprite s, Color c, Vector2 p, int extraOrder)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(holder.transform, false);
        g.transform.position = new Vector3(p.x, p.y, transform.position.z);
        SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.color = c;
        SpriteRenderer birdSr = GetComponent<SpriteRenderer>();
        sr.sortingOrder = (birdSr != null ? birdSr.sortingOrder : 0) - 1 + extraOrder;   // just behind the bird (the cloud is a step in front of the dots)
        return g;
    }
 
    // A small round dot, generated in code
    static Sprite GetDotSprite()
    {
        if (dotSprite != null)
            return dotSprite;
        int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float c = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(size / 2f - dist)));
            }
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        dotSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return dotSprite;
    }
 
    // A small puffy cloud (a few overlapping soft circles), generated in code, 1 unit across
    static Sprite GetCloudSprite()
    {
        if (cloudSprite != null)
            return cloudSprite;
        int size = 64;
        float[] cx = { 22f, 34f, 44f, 32f };
        float[] cy = { 28f, 34f, 27f, 24f };
        float[] cr = { 14f, 17f, 13f, 13f };
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float a = 0f;
                for (int i = 0; i < cx.Length; i++)
                {
                    float dist = Mathf.Sqrt((x - cx[i]) * (x - cx[i]) + (y - cy[i]) * (y - cy[i]));
                    a = Mathf.Max(a, Mathf.Clamp01((cr[i] - dist) / 3f));
                }
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        cloudSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return cloudSprite;
    }
}
public class Block : MonoBehaviour
{
    public float hp, fullHp;
    public Rigidbody2D rb;
    public enum Material
    {
        Glass,
        Wood,
        Stone,
        Iron
    }
    public Material material;
    public float sizeHpExponent = 0.7f;    // how strongly size changes health (0 = not at all, 0.5 = square root, 1 = fully proportional)
    public float minSizeHpFactor = 0.2f;   // a tiny block never gets less than this share of its health
    public float maxSizeHpFactor = 4f;     // a giant block never gets more than this many times its health
    float baseFullHp;                      // health at normal size
    Vector3 lastScale;                     // the size the health was last matched to
    public void Start()
    {
        GetComponent<DestroyableBehaviour>().OverallChance = -1;
        GetComponent<DestroyableBehaviour>().ReactToCollision = false;
        GetComponent<DestroyableBehaviour>().enabled = false;
        rb = GetComponent<Rigidbody2D>();
        //sprite[1] = ModAPI2.LoadSprite(renderer.sprite.name);
        ModAPI.Notify("Activate it to make it stand straight.");
        gameObject.AddComponent<UseEventTrigger>().Action = () =>
        {
            transform.localRotation = Quaternion.Euler(0f, 0f, 90f); // This rotates the magnet to sit up instead of facing right.
        };
        switch (material)
        {
            case Material.Glass:
                fullHp = 7;
                rb.mass = 10;
                break;
            case Material.Wood:
                fullHp = 14;
                rb.mass = 15;
                break;
            case Material.Stone:
                fullHp = 28;
                rb.mass = 30;
                break;
            case Material.Iron:
                fullHp = 56;      // 2x stone
                rb.mass = 60;     // 2x stone
                break;
        }
        if (GetComponent<CircleCollider2D>())
        {
            fullHp *= 2;
            rb.mass *= 2;
        }
        hp = fullHp;
        baseFullHp = fullHp;
        lastScale = transform.localScale;
        ApplySizeScaling();
    }
    void Update()
    {
        // Was the block resized? (a tiny tolerance so nothing triggers by accident)
        if ((transform.localScale - lastScale).sqrMagnitude > 0.000001f)
        {
            lastScale = transform.localScale;
            ApplySizeScaling();
        }
    }
 
    // Matches the block's health to its current size, keeping how damaged it is
    void ApplySizeScaling()
    {
        if (baseFullHp <= 0f)
            return;
 
        // Average of the two scales (a stick stretched in one direction counts as "somewhat bigger")
        float size = (Mathf.Abs(transform.localScale.x) + Mathf.Abs(transform.localScale.y)) * 0.5f;
        float factor = Mathf.Clamp(Mathf.Pow(Mathf.Max(size, 0.01f), sizeHpExponent), minSizeHpFactor, maxSizeHpFactor);
 
        float damageTaken = fullHp > 0f ? Mathf.Clamp01(hp / fullHp) : 1f;   // 1 = undamaged, 0.5 = half health left
        fullHp = baseFullHp * factor;
        hp = fullHp * damageTaken;
    }
    void OnCollisionEnter2D(Collision2D c)
    {
        if (c.gameObject.GetComponent<Rigidbody2D>())
        {
            Rigidbody2D crb = c.gameObject.GetComponent<Rigidbody2D>();
            float force = Utility.Impact(rb, crb);
            if(crb.GetComponent<AngryBird>())
            {
                if (material == Material.Stone || material == Material.Iron)
                {
                    TakeDamage(crb.GetComponent<AngryBird>().stoneMultiplier * Mathf.Min(0.1f * Utility.Impact(rb, crb), 7));
                }
                if (material == Material.Wood)
                {
                    TakeDamage(crb.GetComponent<AngryBird>().woodMultiplier * Mathf.Min(0.1f * Utility.Impact(rb, crb), 7));
                }
                if (material == Material.Glass)
                {
                    TakeDamage(crb.GetComponent<AngryBird>().glassMultiplier * Mathf.Min(0.1f * Utility.Impact(rb, crb), 7));
                }
            }
            else TakeDamage(0.05f * Utility.Impact(rb, crb));
        }
    }
    public void TakeDamage(float damage)
    {
        hp -= damage;
        if (hp < fullHp * 0.5f)
        {
            if (this.gameObject.name == "Glass Block")
            {
                this.gameObject.GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("glasspole2.png", 1f);
            }
            if (this.gameObject.name == "Glass Pole")
            {
                this.gameObject.GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("glassstick2.png", 1f);
            }
            if (this.gameObject.name == "Glass Box")
            {
                this.gameObject.GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("glassbox2.png", 1f);
            }
            if (this.gameObject.name == "Glass Triangle")
            {
                this.gameObject.GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("glassilluminati2.png", 1f);
            }
            if (this.gameObject.name == "Glass Circle")
            {
                this.gameObject.GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("glassrock2.png", 1f);
            }
            if (this.gameObject.name == "Wooden Block")
            {
                this.gameObject.GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("woodblock2.png", 1f);
            }
            if (this.gameObject.name == "Wooden Stick")
            {
                this.gameObject.GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("woodpole2.png", 1f);
            }
            if (this.gameObject.name == "Wooden Box")
            {
                this.gameObject.GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("woodbox2.png", 1f);
            }
            if (this.gameObject.name == "Wooden Triangle")
            {
                this.gameObject.GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("woodtri2.png", 1f);
            }
            if (this.gameObject.name == "Wooden Circle")
            {
                this.gameObject.GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("woodcircle2.png", 1f);
            }
            if (this.gameObject.name == "Stone Block")
            {
                this.gameObject.GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("stoneblock2.png", 1f);
            }
            if (this.gameObject.name == "Stone Stick")
            {
                this.gameObject.GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("stonepole2.png", 1f);
            }
            if (this.gameObject.name == "Stone Box")
            {
                this.gameObject.GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("stonebox2.png", 1f);
            }
            if (this.gameObject.name == "Stone Triangle")
            {
                this.gameObject.GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("stonetri2.png", 1f);
            }
            if (this.gameObject.name == "Stone Circle")
            {
                this.gameObject.GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("stonecircle2.png", 1f);
            }
            if (this.gameObject.name == "Iron Block")
            {
                this.gameObject.GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("ironblock2.png", 1f);
            }
            if (this.gameObject.name == "Iron Stick")
            {
                this.gameObject.GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("ironpole2.png", 1f);
            }
            if (this.gameObject.name == "Iron Box")
            {
                this.gameObject.GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("ironbox2.png", 1f);
            }
            if (this.gameObject.name == "Iron Triangle")
            {
                this.gameObject.GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("irontri2.png", 1f);
            }
            if (this.gameObject.name == "Iron Circle")
            {
                this.gameObject.GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("ironcircle2.png", 1f);
            }
        }
        if (hp < 0)
            Disappear();
    }
    void Disappear()
    {
        GameObject particle;
        particle = Instantiate(GetComponent<DestroyableBehaviour>().DebrisPrefab, transform.position, Quaternion.Euler(0,0,0));
        particle.transform.rotation = Quaternion.Euler(0, 0, 90 + transform.rotation.z);
        foreach (SpriteRenderer e in particle.transform.GetComponentsInChildren<SpriteRenderer>())
        {
            Destroy(e.gameObject.GetComponent<Collider2D>());
            Destroy(e.gameObject.GetComponent<SpriteRenderer>());
            Destroy(e.gameObject, 2f);
        }
        if (material == Material.Glass)
        {
            particle.GetComponent<AudioSource>().clip = null;
            particle.GetComponent<AudioSource>().volume = 1;
            particle.GetComponent<AudioSource>().PlayOneShot(ModAPI2.LoadSound("AudioClip/destroy/Light Destroyed A" + UnityEngine.Random.Range(1, 4) + ".mp3"));
            particle.transform.localScale = Vector2.one * 1.5f;
        }
        if (material == Material.Wood)
        {
            particle.transform.localScale = Vector2.one * 1.3f;
            particle.GetComponent<AudioSource>().clip = null;
            particle.GetComponent<AudioSource>().volume = 1;
            particle.GetComponent<AudioSource>().PlayOneShot(ModAPI2.LoadSound("AudioClip/destroy/Wood Destroyed A" + UnityEngine.Random.Range(1, 4) + ".mp3"));
            var color = particle.GetComponentInChildren<ParticleSystem>().main;
            color.startColor = (Color)new Color32(130, 80, 0, 150);
        }
        if (material == Material.Stone)
        {
            particle.transform.localScale = Vector2.one * 2;
            particle.GetComponent<AudioSource>().clip = null;
            particle.GetComponent<AudioSource>().volume = 1;
            particle.GetComponent<AudioSource>().PlayOneShot(ModAPI2.LoadSound("AudioClip/destroy/Rock Destroyed A" + UnityEngine.Random.Range(1, 4) + ".mp3"));
        }
        if (material == Material.Iron)
        {
            particle.transform.localScale = Vector2.one * 2;
            particle.GetComponent<AudioSource>().clip = null;
            particle.GetComponent<AudioSource>().volume = 1;
            particle.GetComponent<AudioSource>().PlayOneShot(ModAPI2.LoadSound("AudioClip/destroy/Rock Destroyed A" + UnityEngine.Random.Range(1, 4) + ".mp3"));
            var ironColor = particle.GetComponentInChildren<ParticleSystem>().main;
            ironColor.startColor = (Color)new Color32(150, 160, 175, 200);
        }
        Destroy(gameObject);
    }
}
// The Ground block: brown, fixed in place, indestructible
public class GroundBlock : MonoBehaviour
{
    public bool frozenInPlace = true;      // true = doesn't move at all. false = still can't be destroyed, but can be dragged around.
    public float maxTemperature = 30f;     // it never gets hotter than this (so fire can't burn it)
 
    PhysicalBehaviour phy;
    static Sprite sprite = null;
    static float spriteSize = 0f;
 
    // A brown square picture, drawn in code. "size" is how many world units wide the block is.
    public static Sprite MakeSprite(float size)
    {
        if (sprite != null && Mathf.Approximately(spriteSize, size))
            return sprite;
 
        int n = 64;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Point;
 
        Color baseColor = new Color(0.45f, 0.30f, 0.16f, 1f);      // brown
        Color darkColor = new Color(0.30f, 0.19f, 0.09f, 1f);       // darker brown for the speckles and the edge
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float noise = Mathf.PerlinNoise(x * 0.35f + 11f, y * 0.35f + 7f);          // soft patches
                float speck = Mathf.PerlinNoise(x * 1.7f + 3f, y * 1.7f + 19f);            // little dots
                Color c = Color.Lerp(baseColor, darkColor, noise * 0.45f);
                if (speck > 0.78f)
                    c = Color.Lerp(c, darkColor, 0.7f);
                bool edge = x < 2 || y < 2 || x >= n - 2 || y >= n - 2;
                if (edge)
                    c = Color.Lerp(c, darkColor, 0.65f);
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
 
        spriteSize = size;
        sprite = Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), n / size);   // n pixels = "size" world units
        return sprite;
    }
 
    void Start()
    {
        phy = GetComponent<PhysicalBehaviour>();
 
        // Turn off the game's own break-on-impact, the same way your normal blocks do
        DestroyableBehaviour d = GetComponent<DestroyableBehaviour>();
        if (d != null)
        {
            d.OverallChance = -1;
            d.ReactToCollision = false;
            d.enabled = false;
        }
 
        // Grippy, not bouncy: birds and pigs stand on it like on real ground
        PhysicsMaterial2D pm = new PhysicsMaterial2D();
        pm.bounciness = 0f;
        pm.friction = 0.9f;
        Collider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
            col.sharedMaterial = pm;
 
        if (frozenInPlace)
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.bodyType = RigidbodyType2D.Static;
        }
    }
 
    void LateUpdate()
    {
        // Fire can't burn it away
        if (phy != null && phy.Temperature > maxTemperature)
            phy.Temperature = maxTemperature;
    }
}
public static class BlastDamage
{
    public static void Apply(Vector2 center, float radius, float blockDamage, float pigDamage, GameObject ignore)
    {
        HashSet<GameObject> done = new HashSet<GameObject>();
        foreach (Collider2D c in Physics2D.OverlapCircleAll(center, radius))
        {
            if (c == null || c.isTrigger)
                continue;
            GameObject go = c.attachedRigidbody != null ? c.attachedRigidbody.gameObject : c.gameObject;
            if (go == null || go == ignore || go.layer == 11 || !done.Add(go))
                continue;
 
            // 1 right at the blast, fading to 0 at the edge
            float dist = Vector2.Distance(c.bounds.ClosestPoint(center), center);
            float closeness = Mathf.Clamp01(1f - dist / radius);
            if (closeness <= 0f)
                continue;
 
            Block blk = go.GetComponent<Block>();
            if (blk != null)
            {
                blk.TakeDamage(blockDamage * closeness);
                continue;
            }
 
            BadPig pig = go.GetComponent<BadPig>();
            if (pig != null && !pig.isNormallyInvincible)     // El Porkador shrugs it off
                pig.Stun(pigDamage * closeness);
        }
    }
}
public class TNTBlast : MonoBehaviour
{
    public float blastRadius = 7f;     // Bomb: 4
    public float blockDamage = 42f;     // damage to a block right at the blast (Bomb: 16). Hp: glass 7, wood 14, stone 28, iron 56 (circles x2)
    public float pigDamage = 14f;       // Bomb: 8. Pigs have 10 hp
 
    bool quitting = false;
 
    void OnApplicationQuit()
    {
        quitting = true;
    }
 
    // The explosion removes the TNT, so this is the moment it goes off
    void OnDestroy()
    {
        if (quitting || !gameObject.scene.isLoaded)
            return;
        BlastDamage.Apply(transform.position, blastRadius, blockDamage, pigDamage, gameObject);
    }
}
public class Egg : MonoBehaviour
{

}
public class SheetAnimation : MonoBehaviour
{
    public string pathName;
    public int count;
    public Color32 color;
    Sprite[] sprite = { null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null};
    void Start()
    {
        gameObject.AddComponent<SpriteRenderer>();
        GetComponent<SpriteRenderer>().sprite = sprite[0];
        GetComponent<SpriteRenderer>().color = color;
        for (int i = 1; i < count; i++)
            sprite[i] = ModAPI2.LoadSprite("Animation/" + pathName + "/" + (i) + ".png");
        StartCoroutine("E");
    }
    IEnumerator E()
    {
        for (int i = 1; i < count; i++)
        {
            GetComponent<SpriteRenderer>().sprite = sprite[i];
            yield return new WaitForSeconds(0.05f);
        }
        yield return new WaitForSeconds(0.05f);
        Destroy(gameObject);
    }
}
public class SingleAnimation : MonoBehaviour
{
    public Sprite sprite;
    float timer = 0;
    void Start()
    {
        gameObject.AddComponent<SpriteRenderer>();
        GetComponent<SpriteRenderer>().sprite = sprite;
        transform.localScale = Vector2.zero;
    }
    void Update()
    {
        timer += Time.deltaTime;
        if (timer < 0.2f)
            transform.localScale = Vector2.one * timer * 5;
        if (timer > 0.7f)
            transform.localScale = Vector2.one * (0.9f - timer) * 5;
        if (timer > 0.9f)
            Destroy(gameObject);
    }
}
public class Feather : MonoBehaviour
{
    public Color color;
    float timer = 0;
    public float rotationVel;
    void Start()
    {
        gameObject.AddComponent<SpriteRenderer>();
        GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("Feather.png");
        GetComponent<SpriteRenderer>().color = color;
        transform.localScale = Vector2.zero;
    }
    void Update()
    {
        transform.rotation = Quaternion.Euler(0,0,transform.rotation.z + rotationVel * timer);
        timer += Time.deltaTime;
        if (timer < 0.2f)
            transform.localScale = Vector2.one * timer * 5;
        if(timer > 0.2f)
            transform.localScale = Vector2.one * (0.4f - timer) * 5;
        if (timer > 0.4f)
            Destroy(gameObject);
    }
}
public class OrbitFeather : MonoBehaviour
{
    public Transform owner;
    public Color color = Color.white;
    public float radius = 1.1f;
    public float angle = 0f;
    public float speed = 180f;
    public float scale = 0.7f;
    public float fallTime = 1.5f;       // how long a dropped feather lasts before it's gone
 
    SpriteRenderer sr;
    float age = 0f;                     // used for the grow-in
    bool dropped = false;
    float dropTimer = 0f;
    Vector2 velocity;
    float spin;
 
    void Start()
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = ModAPI2.LoadSprite("Feather.png");
        sr.color = color;
        if (owner != null && owner.GetComponent<SpriteRenderer>() != null)
            sr.sortingOrder = owner.GetComponent<SpriteRenderer>().sortingOrder + 1;
        transform.localScale = Vector3.zero;
    }
 
    // Called by MightyRed when he dashes
    public void Drop(Vector2 inheritedVelocity)
    {
        if (dropped)
            return;
        dropped = true;
        float rad = angle * Mathf.Deg2Rad;
        Vector2 tangent = new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad));
        // fly off in the direction it was circling, plus a share of his launch speed, plus a little randomness
        velocity = tangent * radius * speed * Mathf.Deg2Rad
                 + inheritedVelocity
                 + new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(0f, 2f));
        spin = UnityEngine.Random.Range(-360f, 360f);
    }
 
    void Update()
    {
        age += Time.deltaTime;
 
        if (dropped)
        {
            dropTimer += Time.deltaTime;
            velocity += Physics2D.gravity * 0.15f * Time.deltaTime;   // floaty fall
            velocity *= 1f - 1.5f * Time.deltaTime;                   // air drag
            transform.position += (Vector3)(velocity * Time.deltaTime);
            transform.Rotate(0f, 0f, spin * Time.deltaTime);
            Color c = color;
            c.a = 1f - dropTimer / fallTime;                          // fade out
            sr.color = c;
            if (dropTimer >= fallTime)
                Destroy(gameObject);
            return;                                                   // dropped feathers no longer need an owner
        }
 
        if (owner == null)
        {
            Destroy(gameObject);
            return;
        }
        angle += speed * Time.deltaTime;
        float r = angle * Mathf.Deg2Rad;
        transform.position = owner.position + new Vector3(Mathf.Cos(r), Mathf.Sin(r), 0f) * radius;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        transform.localScale = Vector3.one * scale * Mathf.Min(1f, age / 0.3f);   // grow in over 0.3s
    }
}
public class Utility
{
    public static void CreateImage(Sprite sprite, Vector2 pos)
    {
        GameObject e = new GameObject();
        e.transform.position = pos;
        e.AddComponent<SpriteRenderer>();
        e.AddComponent<SingleAnimation>();
        e.GetComponent<SingleAnimation>().sprite = sprite;
    }
    public static void CreateOneShotAudio(AudioClip clip, Vector2 pos, float time)
    {
        GameObject e = new GameObject();
        e.transform.position = pos;
        e.AddComponent<AudioSource>();
        e.GetComponent<AudioSource>().PlayOneShot(clip);
        GameObject.Destroy(e, time);
    }
    public static GameObject CreateFeatherEffect(Color32 color, Vector2 pos)
    {
        GameObject n = new GameObject();
        GameObject[] nn = {new GameObject(), new GameObject(), new GameObject()};
        n.transform.position = pos;
        foreach(GameObject g in nn)
        {
            g.transform.position = n.transform.position + new Vector3(UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f, 0);
            g.AddComponent<Feather>();
            g.GetComponent<Feather>().color = color;
            g.GetComponent<Feather>().rotationVel = (UnityEngine.Random.value - 0.5f) * 2000;
        }
        return n;
    }
    public static GameObject CreateAnimation(string name, int count, Vector2 pos, Color32 color)
    {
        GameObject n = new GameObject();
        n.transform.position = pos;
        SheetAnimation e = n.AddComponent<SheetAnimation>();
        e.color = color;
        e.pathName = name;
        e.count = count;
        return n;
    }
    public static void AddReasonableForceUpdate(Rigidbody2D target, Vector2 e, float force, float massThreshold)
    {
        Vector2 direction = (Vector2)Vector3.Normalize(e);
        if (target.mass < massThreshold)
            target.velocity = target.velocity + e * force * Time.deltaTime;
        else
            target.AddForce(Time.deltaTime * e * force * massThreshold);
    }
    public static void AddReasonableForceImpulse(Rigidbody2D target, Vector2 e, float force, float massThreshold)
    {
        Vector2 direction = (Vector2)Vector3.Normalize(e);
        if (target.mass < massThreshold)
            target.velocity = target.velocity + e * force;
        else
            target.AddForce(e * force * massThreshold, ForceMode2D.Impulse);
    }
    public static void DestroyWires(GameObject c)
    {
        if (c.gameObject.GetComponent<SteelBindingBehaviour>() != null)
            GameObject.Destroy(c.gameObject.GetComponent<SteelBindingBehaviour>());
        if (c.gameObject.GetComponent<WoodenBindingBehaviour>() != null)
            GameObject.Destroy(c.gameObject.GetComponent<WoodenBindingBehaviour>());
        if (c.gameObject.GetComponent<RopeBehaviour>() != null)
            GameObject.Destroy(c.gameObject.GetComponent<RopeBehaviour>());
        if (c.gameObject.GetComponent<WoodStrutBehaviour>() != null)
            GameObject.Destroy(c.gameObject.GetComponent<WoodStrutBehaviour>());
    }
    public static float Impact(Rigidbody2D me, Rigidbody2D other)
    {
        float threshold = 2f;
        float impact = other.mass * other.velocity.magnitude + me.velocity.magnitude;
        if (other.GetComponent<LimbBehaviour>())
            impact = other.mass * other.velocity.magnitude;
        if (me.GetComponent<BadPig>())
        {
            threshold = 2f;
            if (me.GetComponent<BadPig>().AI)
                threshold = 5f;
        }
        if (me.GetComponent<AngryBird>())
        {
            threshold = 2f;
            if (me.GetComponent<AngryBird>().AI)
                threshold = 5f;
        }
        if (impact < threshold)
            return 0;
        return impact;
    }
    public static float Distance(Vector2 t1, Vector2 t2)
    {
        return Mathf.Sqrt((t1.x - t2.x) * (t1.x - t2.x) + (t1.y - t2.y) * (t1.y - t2.y));
    }
    public static float Mass(Rigidbody2D rb)
    {
        if (rb.gameObject.layer == 11)
            return 1;
        return rb.mass;
    }
    public static AngryBird GetNearestBird(Transform transform, float distance)
    {
        AngryBird currentBird = null;
        float currentDistance = distance;
        foreach (AngryBird bird in GameObject.FindObjectsOfType<AngryBird>())
        {
            if (Utility.Distance(transform.position, bird.transform.position) < currentDistance)
                currentBird = bird;
        }
        if (currentBird != null)
            return currentBird;
        return null;
    }

    public static BadPig GetNearestPig(Transform transform, float distance)
    {
        BadPig currentBird = null;
        float currentDistance = distance;

        foreach (BadPig bird in GameObject.FindObjectsOfType<BadPig>())
        {
            if (Utility.Distance(transform.position, bird.transform.position) < currentDistance)
                currentBird = bird;
        }
        if (currentBird != null)
            return currentBird;
        return null;
    }
}
// Originally uploaded by 'Lee Myung-Bak', yes I have permission (I think) to upload this.
