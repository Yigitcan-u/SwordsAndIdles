using System.Collections;
using UnityEngine;

namespace SwordsAndIdles.Combat.View
{
    // Tek bir gladyatorun gorsel tarafi. Kural bilmez; ArenaDirector ne derse onu oynar.
    // Body/Head/Weapon kardes oldugu icin hareket kokten suruluyor, yoksa parcalar dagilir.
    public sealed class GladiatorView : MonoBehaviour
    {
        [SerializeField] private Transform weapon;
        [SerializeField] private Transform healthBar;
        [SerializeField] private Transform stunMarker;
        [SerializeField] private Renderer bodyRenderer;

        [SerializeField] private Color hitColor = new Color(1f, 0.3f, 0.25f);
        [SerializeField] private float defendCrouch = 0.25f;
        [SerializeField] private float restCrouch = 0.12f;

        private Vector3 _home;
        private Quaternion _homeRotation;
        private Quaternion _weaponHome;
        private Vector3 _healthBarFullScale;
        private Color _baseColor;
        private Material _material;
        private float _swayPhase;
        private bool _resting;

        public Vector3 HomePosition => _home;

        private void Awake()
        {
            _home = transform.position;
            _homeRotation = transform.rotation;
            _weaponHome = weapon.localRotation;
            _healthBarFullScale = healthBar.localScale;

            _material = bodyRenderer.material;
            _baseColor = _material.color;

            stunMarker.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (stunMarker.gameObject.activeSelf)
            {
                stunMarker.Rotate(Vector3.up, 360f * Time.deltaTime, Space.World);
            }

            if (!_resting)
            {
                return;
            }

            _swayPhase += Time.deltaTime * 3f;
            transform.position = _home + Vector3.up * (Mathf.Sin(_swayPhase) * 0.04f - restCrouch);
        }

        public void SetHealth(float ratio)
        {
            var scale = _healthBarFullScale;
            scale.x = _healthBarFullScale.x * Mathf.Clamp01(ratio);
            healthBar.localScale = scale;
        }

        public void SetStunned(bool stunned) => stunMarker.gameObject.SetActive(stunned);

        public void ClearPose()
        {
            _resting = false;
            transform.position = _home;
            weapon.localRotation = _weaponHome;
        }

        public void SetDefendPose()
        {
            _resting = false;
            transform.position = _home - Vector3.up * defendCrouch;
            weapon.localRotation = _weaponHome * Quaternion.Euler(0f, 0f, 55f);
        }

        public void SetRestPose()
        {
            _resting = true;
            _swayPhase = 0f;
            weapon.localRotation = _weaponHome * Quaternion.Euler(0f, 0f, -60f);
        }

        public IEnumerator Lunge(Vector3 towards, float distance, float duration)
        {
            var direction = Flatten(towards - _home, Vector3.forward);
            var peak = _home + direction * distance;

            yield return MoveTo(_home, peak, duration * 0.4f);
            yield return MoveTo(peak, _home, duration * 0.6f);
        }

        public IEnumerator Recoil(Vector3 away, float strength)
        {
            var direction = Flatten(_home - away, Vector3.back);
            var back = _home + direction * strength;

            StartCoroutine(FlashHit());

            yield return MoveTo(_home, back, 0.08f);
            yield return MoveTo(back, _home, 0.16f);
        }

        public IEnumerator Sidestep()
        {
            var aside = _home + Vector3.forward * 0.4f;

            yield return MoveTo(_home, aside, 0.1f);
            yield return MoveTo(aside, _home, 0.15f);
        }

        public IEnumerator Topple()
        {
            _resting = false;

            var fallen = _homeRotation * Quaternion.Euler(0f, 0f, 90f);
            var elapsed = 0f;
            const float duration = 0.6f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.rotation = Quaternion.Slerp(_homeRotation, fallen, elapsed / duration);
                yield return null;
            }

            transform.rotation = fallen;
        }

        private static Vector3 Flatten(Vector3 direction, Vector3 fallback)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : fallback;
        }

        private IEnumerator FlashHit()
        {
            var elapsed = 0f;
            const float duration = 0.25f;

            _material.color = hitColor;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _material.color = Color.Lerp(hitColor, _baseColor, elapsed / duration);
                yield return null;
            }

            _material.color = _baseColor;
        }

        private IEnumerator MoveTo(Vector3 from, Vector3 to, float duration)
        {
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            transform.position = to;
        }
    }
}
