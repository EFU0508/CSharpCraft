using ModelLib;
using System;

namespace GameLabo
{
    /// <summary>
    /// NPCステージ共通の基底クラス
    /// 各ステージ固有のNPC処理はこのクラスを継承して実装する
    /// </summary>
    public class NpcStageBase : IDisposable
    {
        /// <summary>
        /// 重力加速度（将来的にNPCの物理挙動で使用する想定）
        /// 単位は m/s^2 相当
        /// </summary>
        public const float gravity = 9.8f;

        /// <summary>
        /// このステージに存在するNPCの情報配列
        /// ModelInfo にはモデルハンドル・座標・アニメ情報などが含まれる
        /// </summary>
        public ModelInfo[] NpcInfo { get; set; }

        /// <summary>
        /// コンストラクタ
        /// ※ 基本的には派生クラス側で初期化を行う
        /// </summary>
        public NpcStageBase() { }

        /// <summary>
        /// リソース解放用
        /// 派生クラスで ModelInfo や独自リソースを Dispose する
        /// </summary>
        public virtual void Dispose() { }

        /// <summary>
        /// ステージ開始時の初期化処理
        /// NPC生成・初期配置などを行う
        /// </summary>
        public virtual void Init() { }

        /// <summary>
        /// ステージ終了時の終了処理
        /// Initで確保したリソースの後始末などを行う
        /// </summary>
        public virtual void Term() { }

        /// <summary>
        /// 毎フレーム呼ばれるロジック処理
        /// NPCのAI・移動・アニメ更新などを行う
        /// </summary>
        public virtual void Logic() { }

        /// <summary>
        /// 毎フレーム呼ばれる描画処理
        /// NPCモデルの描画などを行う
        /// </summary>
        public virtual void Draw() { }
    }
}
