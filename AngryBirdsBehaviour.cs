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
public class Red : MonoBehaviour
{
    CircleCollider2D range;
    bool ability = false;
    AngryBird bird;
    void Start()
    {
        bird = GetComponent<AngryBird>();
        if (!GetComponent<CircleCollider2D>() || !GetComponent<CircleCollider2D>().isTrigger)
        {
            range = gameObject.AddComponent<CircleCollider2D>();
            range.isTrigger = true;
            range.radius = 5f;
        }
        else
            range = GetComponent<CircleCollider2D>();
        if (!gameObject.GetComponent<TriggerEventBehaviour>())
        {
            bird.sprite[2] = bird.sprite[1];
            gameObject.AddComponent<UseEventTrigger>().Action = () =>
            {
                ability = true;
                bird.Stun(1);
                bird.mouth.clip = bird.use[UnityEngine.Random.Range(1, 4)];
                bird.mouth.Play();
                Invoke("Deactivate", 1);
                ModAPI.CreateParticleEffect("Disintegration", transform.position);
            };
        }
    }
    void Deactivate()
    {
        ability = false;
    }
    void OnTriggerStay2D(Collider2D collision)
    {
        if (ability == false)
            return;
        if (Utility.Distance(collision.transform.position, transform.position) > 5)
            return;
        if(collision.GetComponent<Rigidbody2D>().mass < 10)
            collision.gameObject.GetComponent<Rigidbody2D>().velocity += (Vector2)(collision.gameObject.transform.position - transform.position) * 8 * Time.deltaTime;
        else
            collision.gameObject.GetComponent<Rigidbody2D>().AddForce((Vector2)(collision.gameObject.transform.position - transform.position) * 80 * Time.deltaTime, ForceMode2D.Impulse);
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
 
    IEnumerator Dash()
    {
        active = true;
		if (onlyWhileCharging) SpawnFeathers();
        float originalMass = bird.rb.mass;
        float originalGravity = bird.rb.gravityScale;
 
        // ---- Phase 1: hover in place ----
        bird.enableSelfBalancing = false;   // stop the "stand upright" torque while charging
        bird.rb.gravityScale = 0f;
        bird.mouth.clip = bird.use[UnityEngine.Random.Range(1, 4)];
        bird.mouth.Play();
        Utility.CreateFeatherEffect(new Color32(255, 0, 0, 255), transform.position);
 
        float t = 0f;
        while (t < pauseTime)
        {
            bird.rb.velocity = Vector2.zero;
            bird.rb.angularVelocity = 0f;
            t += Time.deltaTime;
            yield return null;
        }
 
        // ---- Phase 2: aim at the mouse RIGHT NOW (after the pause) and launch ----
        Vector2 aimPoint = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (bird.AI && bird.target != null)          // with AI on, dash at his target instead
            aimPoint = bird.target.transform.position;
 
        Vector2 dir = (aimPoint - (Vector2)transform.position).normalized;
        if (dir == Vector2.zero)
            dir = Vector2.right;
 
        bird.wireAction = AngryBird.WireAction.DestroyOnHighSpeed;
        bird.rb.mass = originalMass * 3f;            // heavier = hits harder (same trick as Chuck/Hal)
        bird.rb.velocity = dir * dashSpeed;
		DropFeathers();
        ModAPI.CreateParticleEffect("Flash", transform.position);
 
        yield return new WaitForSeconds(dashDuration);
 
        // ---- Phase 3: back to normal ----
        bird.rb.mass = originalMass;
        bird.rb.gravityScale = originalGravity;
        bird.enableSelfBalancing = true;
		if (regrowFeathers && !onlyWhileCharging) SpawnFeathers();
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
        bird.rb.mass = 2;
    }
}
public class Bomb : MonoBehaviour
{
    bool activated = false;
    AngryBird bird;
    void Start()
    {
        bird = GetComponent<AngryBird>();
        gameObject.AddComponent<UseEventTrigger>().Action = () =>
        {
            bird.mouth.clip = bird.use[UnityEngine.Random.Range(1, 4)];
            bird.mouth.Play();
            GetComponent<ExplosiveBehaviour>().Activate();
            StartCoroutine("Activate");
        };
    }
    IEnumerator Activate()
    {
        Utility.CreateAnimation("Explosion", 15, transform.position, new Color32(255,255,255,255)).transform.localScale = 2 * Vector2.one;
        activated = true;
        GetComponent<CircleCollider2D>().enabled = false;
        GetComponent<CircleCollider2D>().enabled = true;
        yield return new WaitForSeconds(0.1f);
        activated = false;
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
        if (bird.rb.velocity.magnitude < 10)
            return;
        if (c.gameObject.CompareTag("Player"))
            return;
        if (c.gameObject.layer != 11)
        {
            if (c.gameObject.GetComponent<DestroyableBehaviour>() == null && c.gameObject.GetComponent<LimbBehaviour>() == null && c.gameObject.tag != "Player" && !c.gameObject.GetComponent<DebrisComponent>())
            {
                Destroy(c.gameObject);
                ModAPI.CreateParticleEffect("Explosion", transform.position);
            }
        }
        else
        {
            explosionSource.Activate();
        }
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
    void Start()
    {
        bird = GetComponent<AngryBird>();
        if (!gameObject.GetComponent<TriggerEventBehaviour>())
        {
            gameObject.AddComponent<UseEventTrigger>().Action = () =>
            {
                bird.rb.velocity = new Vector2(bird.rb.velocity.x, 16);
                bird.mouth.clip = bird.use[UnityEngine.Random.Range(1, 4)];
                bird.mouth.Play();
                GameObject e = Instantiate(ModAPI.FindSpawnable("Brick").Prefab);
                e.transform.position = transform.position + new Vector3(0, -1f, 0);
                e.GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("Egg.png", 1.5f);
                e.gameObject.AddComponent<ExplosiveBehaviour>();
                e.gameObject.GetComponent<ExplosiveBehaviour>().Range = 16f;
                e.gameObject.GetComponent<ExplosiveBehaviour>().FragmentForce = 30f;
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
        bird.rb.mass = 2;
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
                ModAPI.CreateParticleEffect("BigExplosion", transform.position);
                phase = 0;
                transform.localScale = Vector2.one;
                bird.isTerence = false;
                GetComponent<SpriteRenderer>().sprite = bird.sprite[0];
                bird.rb.mass = 1;
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
    float timer = -1;
    void Start()
    {
        bird = GetComponent<AngryBird>();
        gameObject.AddComponent<UseEventTrigger>().Action = () =>
        {
            bird.Say(bird.use);
            Activate();
        };
        phy = GetComponent<PhysicalBehaviour>();
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
        timer = 0.1f;
        CircleCollider2D triggerCollider;
        triggerCollider = GetComponent<CircleCollider2D>();
        triggerCollider.enabled = false;
        triggerCollider.enabled = true;
    }
    void Update()
    {
        if (timer > 0)
            timer -= Time.deltaTime;
        phy.Temperature = -30;
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
        c.gameObject.GetComponent<PhysicalBehaviour>().Temperature = -1000;
        if (c.gameObject.GetComponent<DamagableMachineryBehaviour>())
            c.gameObject.GetComponent<DamagableMachineryBehaviour>().Break(Vector2.zero);
        if (c.gameObject.GetComponent<Block>())
        {
            if (c.gameObject.GetComponent<Block>().material == Block.Material.Glass)
                return;
            GameObject block = c.gameObject;
            SpawnableAsset asset = ModAPI.FindSpawnable("Glass Block");
            if (block.name == "Wooden Block" || block.name == "Stone Block" || block.name == "Iron Block")
            {
                block.name = "Glass Block";
                block.GetComponent<Block>().hp = 7;
                block.GetComponent<Block>().fullHp = 7;
                block.GetComponent<Block>().rb.mass = 15;
                block.GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("glasspole.png");
            }
            if (block.name == "Wooden Stick" || block.name == "Stone Stick" || block.name == "Iron Stick")
            {
                block.name = "Glass Pole";
                block.GetComponent<Block>().hp = 7;
                block.GetComponent<Block>().fullHp = 7;
                block.GetComponent<Block>().rb.mass = 15;
                block.GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("glassstick.png");
            }
            if (block.name == "Wooden Box" || block.name == "Stone Box" || block.name == "Iron Box")
            {
                block.name = "Glass Box";
                block.GetComponent<Block>().hp = 7;
                block.GetComponent<Block>().fullHp = 7;
                block.GetComponent<Block>().rb.mass = 15;
                block.GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("glassbox.png");
            }
            if (block.name == "Wooden Triangle" || block.name == "Stone Triangle" || block.name == "Iron Triangle")
            {
                block.name = "Glass Triangle";
                block.GetComponent<Block>().hp = 7;
                block.GetComponent<Block>().fullHp = 7;
                block.GetComponent<Block>().rb.mass = 15;
                block.GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("glassilluminati.png");
            }
            if (block.name == "Wooden Circle" || block.name == "Stone Circle" || block.name == "Iron Circle")
            {
                block.name = "Glass Circle";
                block.GetComponent<Block>().hp = 14;
                block.GetComponent<Block>().fullHp = 14;
                block.GetComponent<Block>().rb.mass = 30;
                block.GetComponent<SpriteRenderer>().sprite = ModAPI2.LoadSprite("glassrock.png");
            }
            block.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("ice");
            block.GetComponent<Block>().material = Block.Material.Glass;
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
            case "Chuck":
                personalColor = new Color32(255, 255, 0, 255);
                break;
            case "Bomb":
                personalColor = new Color32(0, 0, 0, 255);
                break;
            case "Shockwave":
                personalColor = new Color32(0, 200, 255, 255);
                break;
            case "Blues":
                personalColor = new Color32(0, 200, 255, 255);
                break;
            case "Matilda":
                personalColor = new Color32(255, 255, 255, 255);
                break;
            case "Terence":
                personalColor = new Color32(200, 0, 0, 255);
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
            case "SpaceEgg":
                personalColor = new Color32(200, 255, 255, 255);
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

    void FixedUpdate()
    {
        if (stat == Status.SlingFly)
        {
            rotZ = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, rotZ);
        }
    }
    void OnCollisionEnter2D(Collision2D c)
    {
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
        //Utility.CreateOneShotAudio(ModAPI2.LoadSound("AudioClip/bird destroyed.ogg"), transform.position, 2);
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
            ModAPI.CreateParticleEffect("BloodExplosion", transform.position).transform.localScale = Vector3.one * 1.2f;
        }
        enableSelfBalancing = false;
    }
    void Die()
    {
        Utility.CreateAnimation("Pop", 8, transform.position, new Color32(100, 255, 100, 255));
        Utility.CreateImage(ModAPI2.LoadSprite("5000.png"), transform.position + new Vector3(0, 1.5f));
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
		ModAPI.Notify("Slingshot touched: " + c.gameObject.name + " | AngryBird: " + (c.gameObject.GetComponent<AngryBird>() != null));
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
    // ---- Aiming ----
    public bool aimWithMouse = true;     // true = shoot toward the mouse; false = fixed launchAngle
    public float launchSpeed = 30f;      // every bird leaves at exactly this speed
    public float launchAngle = 45f;      // only used when aimWithMouse is false (degrees, in the slingshot's own space)
    public bool showGuide = true;        // dotted flight path while a bird is in the cup
    public int guideDots = 24;
    public float guideStep = 0.02f;      // seconds of flight between dots (bigger = longer guide)
 
    // ---- Where the ready bird sits (in the slingshot's own space; mirrors if the slingshot is flipped) ----
    public Vector2 cupOffset = new Vector2(0f, 0.5f);       // the bird that's ready to fire
    public int maxQueued = 27;            // how many can wait on the ground (so 28 birds in total, counting the cup)
    public float maxWaitDistance = 28f;  // a waiting bird that gets knocked/walks further away than this is forgotten
 
    // ---- Reloading ----
    public float reloadDelay = 1.5f;     // seconds after a shot before the next bird hops on
    public float hopTime = 0.7f;        // how long a hop takes
    public float hopHeight = 2f;       // how high a hop arcs
 
    // ---- Misc ----
    public bool activateOnce = true;     // X uses the last shot bird's power once
    public float catchDistance = 0.1f;   // how close a bird must be to the slingshot's body to be caught
    public float aimDeadZone = 1.5f;     // cursor closer than this to the cup = keep the last aim (so clicking the slingshot doesn't aim at it)
 
    // The bird that's in the cup (or hopping into it)
    class Rec
    {
        public AngryBird bird;
        public Vector2 cur;       // where it is, in the slingshot's own space
        public Vector2 from;      // where its current hop started
        public Vector2 target;    // where it's heading
        public float t = 1f;      // hop progress, 0..1 (1 = arrived)
    }
 
    Rec cup = null;
    // Waiting birds are NOT frozen: they stay on the ground as normal physics objects
    List<AngryBird> waiting = new List<AngryBird>();
    Dictionary<AngryBird, float> blockedUntil = new Dictionary<AngryBird, float>();   // so a just-shot bird isn't re-caught
    AngryBird lastShot = null;
    bool lastShotUsed = false;
    float reloadTimer = 0f;
    float scanTimer = 0f;
    bool hinted = false;
    Vector2 lastAim = Vector2.right;
    bool hasAim = false;
 
    GameObject[] dots = null;
    static Sprite dotSprite = null;
 
    void Start()
    {
        // Launch = the normal Activate/Use key while the slingshot is selected
        if (!gameObject.GetComponent<TriggerEventBehaviour>())
        {
            gameObject.AddComponent<UseEventTrigger>().Action = () =>
            {
                LaunchCup();
            };
        }
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
    // PLACING AND HOPPING THE CUP BIRD
    // ------------------------------------------------------------
    void LateUpdate()
    {
        if (cup != null && cup.bird == null)
            cup = null;
 
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
 
        if (cup != null)
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
        }
        if (r.t < 1f)
            r.t = Mathf.Min(1f, r.t + Time.deltaTime / Mathf.Max(0.01f, hopTime));
 
        float arc = 4f * r.t * (1f - r.t) * hopHeight;      // 0 -> peak -> 0
        r.cur = Vector2.Lerp(r.from, r.target, r.t);
        Vector3 world = transform.TransformPoint(r.cur + Vector2.up * arc);
        r.bird.transform.position = new Vector3(world.x, world.y, r.bird.transform.position.z);
        r.bird.transform.rotation = transform.rotation;
    }
 
    // ------------------------------------------------------------
    // AIMING
    // ------------------------------------------------------------
    Vector2 AimDirection()
    {
        Vector2 cupWorld = (Vector2)transform.TransformPoint(cupOffset);
        if (aimWithMouse && Camera.main != null)
        {
            Vector2 mouse = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 d = mouse - cupWorld;
            if (d.magnitude >= aimDeadZone)
            {
                lastAim = d.normalized;
                hasAim = true;
                return lastAim;
            }
            if (hasAim)
                return lastAim;      // cursor is on/near the slingshot: keep the aim you had
        }
        // Fixed angle in the slingshot's own space (rotate or flip the slingshot to re-aim)
        float a = launchAngle * Mathf.Deg2Rad;
        return ((Vector2)transform.TransformVector(new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f))).normalized;
    }
 
    // A dotted preview of the flight path (ignores obstacles)
    void UpdateGuide()
    {
        bool show = showGuide && cup != null && cup.bird != null && cup.t >= 1f;
 
        if (dots == null)
        {
            if (!show)
                return;
            dots = new GameObject[guideDots];
            Sprite dot = GetDotSprite();
            for (int i = 0; i < guideDots; i++)
            {
                GameObject g = new GameObject("AimDot");
                SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
                sr.sprite = dot;
                sr.sortingOrder = 60;
                sr.color = new Color(1f, 1f, 1f, Mathf.Max(0.15f, 0.9f - 0.05f * i));
                g.transform.localScale = Vector3.one * Mathf.Max(0.4f, 1f - 0.04f * i);
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
 
        Vector2 p0 = (Vector2)transform.TransformPoint(cupOffset);
        Vector2 v = AimDirection() * launchSpeed;
        Rigidbody2D rb = cup.bird.GetComponent<Rigidbody2D>();
        Vector2 gravity = Physics2D.gravity * (rb != null ? rb.gravityScale : 1f);
 
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
    void LaunchCup()
    {
        // Only fire a bird that has finished hopping into the cup
        if (cup == null || cup.bird == null || cup.t < 1f)
            return;
 
        AngryBird b = cup.bird;
        Rigidbody2D rb = b.GetComponent<Rigidbody2D>();
        if (rb == null)
            return;
 
        Vector2 dir = AimDirection();
        cup = null;
        reloadTimer = reloadDelay;          // the next bird hops on after this long
 
        StartCoroutine(IgnoreBriefly(b, 0.5f));   // don't bump into the slingshot on the way out
        blockedUntil[b] = Time.time + 1f;
 
        rb.simulated = true;
        if (b.gameMass > 0f)
            rb.mass = b.gameMass;                 // the heavier "in flight" mass every bird normally has
        rb.velocity = dir * launchSpeed;          // identical for every bird, whatever its mass
        rb.angularVelocity = 0f;
 
        b.stat = AngryBird.Status.SlingFly;
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
    // RELOADING + X
    // ------------------------------------------------------------
    void Update()
    {
        if (InputSystem.Down("Slingshot X: launch"))
            LaunchCup();
 
        // Reload: once the delay has passed, the closest waiting bird hops into the empty cup
        if (reloadTimer > 0f)
            reloadTimer -= Time.deltaTime;
        if (cup == null && reloadTimer <= 0f && waiting.Count > 0)
        {
            AngryBird next = PickNextBird();
            if (next != null)
                MoveToCup(next);
        }
 
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
    void TakeDamage(float damage)
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
// Originally uploaded by 'Lee Myung-Bak'. Do not reupload without their explicit permission.
