using System;
using static DX;

namespace ModelLib
{
    /// <summary>
    /// キャラクター / NPC / オブジェクト共通のモデル情報クラス
    /// 描画・移動・アニメーション・当たり判定・各種状態を保持する
    /// </summary>
    public class ModelInfo : IDisposable
    {
        // =========================
        // 基本状態
        // =========================

        /// <summary>動作モード（用途はゲーム側で定義）</summary>
        public int Mode;

        /// <summary>DxLibのモデルハンドル</summary>
        public int Handle;

        /// <summary>現在の座標</summary>
        public VECTOR Position;

        /// <summary>次フレームの予定座標</summary>
        public VECTOR NewPosition;

        /// <summary>前フレームの座標</summary>
        public VECTOR PrePosition;

        /// <summary>モデルのスケール</summary>
        public VECTOR Scale;

        /// <summary>モデルの回転（XYZ）</summary>
        public VECTOR Rotation;

        // =========================
        // 入力状態（主にプレイヤー用）
        // =========================

        public int ButtonA;
        public int ButtonB;
        public int ButtonX;
        public int ButtonY;

        // =========================
        // 移動関連
        // =========================

        /// <summary>移動先の目標座標</summary>
        public VECTOR TargetPos;

        /// <summary>移動に使用するタイマー</summary>
        public float MoveTimer;

        /// <summary>移動中かどうか</summary>
        public bool IsMoving;

        // =========================
        // 当たり判定・サイズ情報
        // =========================

        /// <summary>モデルAABB最小</summary>
        public VECTOR MminV;

        /// <summary>モデルAABB最大</summary>
        public VECTOR MmaxV;

        /// <summary>モデルの高さ</summary>
        public float Height;

        /// <summary>モデルの幅</summary>
        public float Width;

        /// <summary>カプセル判定の中心</summary>
        public VECTOR CapsuleC;

        /// <summary>カプセル判定の半径</summary>
        public float CapsuleR;

        /// <summary>カプセル判定の高さ</summary>
        public float CapsuleH;

        // =========================
        // 時間・速度
        // =========================

        /// <summary>内部時間</summary>
        public float Time;

        /// <summary>移動速度</summary>
        public float Speed;

        // =========================
        // ジャンプ関連
        // =========================

        /// <summary>ジャンプ中かどうか</summary>
        public bool isJumping;

        /// <summary>Y方向の速度（重力計算用）</summary>
        public float verticalVelocity;

        // =========================
        // アニメーション関連
        // =========================

        /// <summary>現在のアニメーション番号</summary>
        public int AnimeIndex;

        /// <summary>前回のアニメーション番号</summary>
        public int OldAnimeIndex;

        /// <summary>アタッチ中のフレーム番号</summary>
        public int AttachIndex;

        /// <summary>アニメーション再生時間</summary>
        public float AnimeTime;

        /// <summary>アニメーション総時間</summary>
        public float TotalTime;

        /// <summary>現在の再生時間</summary>
        public float PlayTime;

        /// <summary>ループ再生するか</summary>
        public bool Repeat;

        // =========================
        // 表情・外見
        // =========================

        /// <summary>表情インデックス</summary>
        public int FaceIndex;

        /// <summary>モデル種別</summary>
        public int ModelType;

        /// <summary>着せ替え：上</summary>
        public int DressUpTop;

        /// <summary>着せ替え：上半身</summary>
        public int DressUpUpper;

        /// <summary>着せ替え：下半身</summary>
        public int DressUpLower;

        // =========================
        // 状態値
        // =========================

        /// <summary>地面に接地しているか</summary>
        public bool IsGround;

        /// <summary>体力</summary>
        public int HP;

        /// <summary>ユーザー名（オンライン / 表示用）</summary>
        public string UserName;

        // =========================
        // 釣り関連
        // =========================

        public int FishingStatus;
        public VECTOR LurePos;
        public bool isFishingStatus;
        public float FishingRotY;
        public float FishingTime;
        public VECTOR rodTip;
        public VECTOR FishingRodPos;
        public VECTOR FishingRodRot;
        public VECTOR FishingMidpoint;
        public int HookHandle;

        // =========================
        // 狩猟（弓）関連
        // =========================

        public int HunterStatus;
        public VECTOR BowPosition;
        public VECTOR BowRotation;
        public int ArrowHandle;
        public int ArrowAlive;
        public VECTOR ArrowPosition;
        public VECTOR ArrowRotation;
        public VECTOR ArrowMidpoint;

        // =========================
        // 獲物関連
        // =========================

        public int PreyType;
        public int PreyScore;

        /// <summary>
        /// コンストラクタ（初期状態の設定）
        /// </summary>
        public ModelInfo()
        {
            Mode = 0;

            Speed = 0;
            isJumping = true;

            MoveTimer = 0f;
            IsMoving = false;

            Time = 0f;

            AttachIndex = -1;
            AnimeTime = 0f;
            TotalTime = 0f;
            PlayTime = 0f;
            Repeat = false;

            Handle = -1;
            ArrowHandle = -1;

            Position = VGet(0f, 0f, 0f);
            NewPosition = VGet(0f, 0f, 0f);
            PrePosition = VGet(0f, 0f, 0f);
            Scale = VGet(1f, 1f, 1f);
            Rotation = VGet(0f, 0f, 0f);
            AnimeIndex = 0;
            OldAnimeIndex = -1;
            FaceIndex = -1;
            ModelType = -1;
            DressUpTop = 0;
            DressUpUpper = 0;
            DressUpLower = 0;

            ButtonA = FALSE;
            ButtonB = FALSE;
            ButtonX = FALSE;
            ButtonY = FALSE;

            IsGround = false;

            HP = 1;

            UserName = string.Empty;

            FishingStatus = 0;
            HunterStatus = 0;
            ArrowAlive = FALSE;

            PreyType = 0;
            PreyScore = 0;
        }

        /// <summary>
        /// モデルリソースの解放
        /// </summary>
        public void Dispose()
        {
            MV1DeleteModel(Handle);
            MV1DeleteModel(ArrowHandle);
            MV1DeleteModel(HookHandle);
        }

        /// <summary>
        /// モデルを複製して使用する（キャラ / 矢 / フック）
        /// </summary>
        /// <param name="_Handle">元モデルのハンドル</param>
        /// <param name="CharaType">キャラクター種別</param>
        /// <param name="_ArrowHandle">矢モデル（任意）</param>
        /// <param name="_HookHandle">フックモデル（任意）</param>
        public void DuplicateModel(int _Handle, int CharaType, int _ArrowHandle = -1, int _HookHandle = -1)
        {
            Handle = MV1DuplicateModel(_Handle);
            MV1SetupCollInfo(Handle);
            ModelType = CharaType;

            if (_ArrowHandle >= 0)
            {
                ArrowHandle = MV1DuplicateModel(_ArrowHandle);
                MV1SetupCollInfo(ArrowHandle);
            }

            if (_HookHandle >= 0)
            {
                HookHandle = MV1DuplicateModel(_HookHandle);
                MV1SetupCollInfo(HookHandle);
            }
        }
    }
}
