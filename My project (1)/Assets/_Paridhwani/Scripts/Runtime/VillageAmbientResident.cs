using UnityEngine;

namespace Paridhwani
{
    [RequireComponent(typeof(CharacterController))]
    [DefaultExecutionOrder(-20)]
    public sealed class VillageAmbientResident : MonoBehaviour
    {
        public Vector3[] route;
        public float pace = 1.05f, pause = 3;
        public int firstStop;
        public float TravelDistance { get; private set; }
        public float Speed { get; private set; }
        public Vector3 Velocity { get; private set; }
        public bool Grounded => motor && motor.isGrounded;
        public bool Stopped { get; private set; }
        public bool ReducedMotion => session && session.player && session.player.reducedMotion;
        CharacterController motor;
        TeaStallSession session;
        VillageCharacter character;
        int stop;
        float waiting, gravity;
        Vector3 movement;

        void Start()
        {
            motor = GetComponent<CharacterController>();
            character = GetComponentInChildren<VillageCharacter>();
            session = FindAnyObjectByType<TeaStallSession>();
            stop = route == null || route.Length == 0 ? 0 : firstStop % route.Length;
            waiting = pause * .35f;
            if (character && session) character.lookTarget = session.player.transform;
        }

        void Update()
        {
            if (!session) session = FindAnyObjectByType<TeaStallSession>();
            Stopped = !Application.isFocused || !session || !session.enabled || !session.ui ||
                session.ui.IsModal || session.player.IsTransitioning || route == null || route.Length < 2;
            // Ambient life is suspended outside the active neighbourhood and during menus.
            if (session && (session.player.transform.position - transform.position).sqrMagnitude > 65 * 65) Stopped = true;
            if (Stopped) { movement = Velocity = Vector3.zero; Speed = 0; return; }
            float dt = Mathf.Min(Time.deltaTime, .05f);
            Vector3 delta = route[stop] - transform.position; delta.y = 0;
            if (delta.sqrMagnitude < .12f * .12f && waiting <= 0)
            { stop = (stop + 1) % route.Length; waiting = pause; }
            waiting = Mathf.Max(0, waiting - dt);
            bool yielding = false;
            Vector3 player = session.player.transform.position - transform.position; player.y = 0;
            if (player.sqrMagnitude < 1.3f * 1.3f && Vector3.Dot(player, delta) > 0) yielding = true;
            var desired = waiting > 0 || yielding ? Vector3.zero : delta.normalized * Mathf.Min(pace, delta.magnitude / Mathf.Max(.001f, dt));
            movement = Vector3.MoveTowards(movement, desired, dt * 3.5f);
            if (movement.sqrMagnitude > .01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(movement), dt * 110);
            gravity = motor.isGrounded ? -2 : Mathf.Max(-12, gravity - 18 * dt);
            Vector3 before = transform.position;
            motor.Move((movement + Vector3.up * gravity) * dt);
            Vector3 travelled = transform.position - before; travelled.y = 0;
            Velocity = dt > 0 ? travelled / dt : Vector3.zero; Speed = Velocity.magnitude;
            TravelDistance += travelled.magnitude;
            if (desired.sqrMagnitude > .1f && Speed < .03f) movement = Vector3.zero;
        }

        void OnDisable() { movement = Velocity = Vector3.zero; Speed = 0; Stopped = true; }
    }
}
