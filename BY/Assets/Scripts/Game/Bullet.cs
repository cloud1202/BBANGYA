using System;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float _speed = 20f;
    [SerializeField] private float _lifeTime = 3f;

    private string _ownerName;
    private Action<Bullet> _onReload;
    private bool _isFiring = false;
    private Rigidbody2D _rb2D;
    private Vector2 _direction;
    private float _timer;

    private void Awake()
    {
        _rb2D = GetComponent<Rigidbody2D>();
    }

    public void Init(string ownerName, Action<Bullet> onReload)
    {
        _ownerName = ownerName;
        _onReload = onReload;
        gameObject.SetActive(false);
    }

    public void Firing(Transform muzzle, Vector2 direction)
    {
        transform.position = muzzle.position;
        transform.right = direction;
        _direction = direction;
        _isFiring = true;
        _timer = 0f;
        gameObject.SetActive(true);

        if (_rb2D != null)
            _rb2D.linearVelocity = _direction * _speed;
    }

    private void Update()
    {
        if (!_isFiring) return;

        _timer += Time.deltaTime;
        if (_timer >= _lifeTime)
            Reload();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_isFiring) return;

        // 오너 자신은 무시
        if (collision.gameObject.name == _ownerName) return;

        // 플레이어 히트
        PlayerController player = collision.GetComponent<PlayerController>();
        if (player != null)
        {
            player.TakeDamageServerRpc(1f);
        }

        Reload();
    }

    private void Reload()
    {
        _isFiring = false;
        gameObject.SetActive(false);

        if (_rb2D != null)
            _rb2D.linearVelocity = Vector2.zero;

        _onReload?.Invoke(this);
    }
}
