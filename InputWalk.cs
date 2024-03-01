using UnityEngine;

namespace Game
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class InputWalk : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 2f;
        private Rigidbody2D _rigidbody2D;
        private Animator _animator;
        private static readonly int HASH_X = Animator.StringToHash("X");
        private static readonly int HASH_Y = Animator.StringToHash("Y");
        
        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            //  プレイヤーの入力を取得
            var axis = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            
            //  なにも入力されていない
            if (axis == Vector2.zero)
            {
                //  停止処理をし、以降の処理を実行せずに終わる
                StopWalk();
                return;
            }
            
            //  入力されているがアニメーション速度が0（歩き始め）
            if(_animator.speed == 0) StartWalk();
            
            //  アニメーションの更新
            _animator.SetFloat(HASH_X, axis.x);
            _animator.SetFloat(HASH_Y, axis.y);

            //  移動速度を設定
            _rigidbody2D.velocity = axis * _moveSpeed;
        }

        /// <summary>
        /// 移動を止める
        /// </summary>
        private void StopWalk()
        {
            //  速度を0にし、さらに物理演算の座標と角度をロックする
            _rigidbody2D.velocity = Vector2.zero;
            _rigidbody2D.constraints = RigidbodyConstraints2D.FreezeAll;

            //  アニメーションを90%の位置に強制移動して速度を0にすることで待機扱いにする
            var currentStateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            _animator.Play(currentStateInfo.fullPathHash, 0, 0.9f);
            _animator.speed = 0;
        }

        /// <summary>
        /// 歩行を開始する
        /// </summary>
        private void StartWalk()
        {
            //  物理演算の座標ロックを解除する
            _rigidbody2D.constraints = RigidbodyConstraints2D.FreezeRotation;

            //  アニメーションを0%の位置に強制移動して速度を1に戻すことで歩き始め扱いにする
            var currentStateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            _animator.Play(currentStateInfo.fullPathHash, 0, 0);
            _animator.speed = 1;
        }
    }
}