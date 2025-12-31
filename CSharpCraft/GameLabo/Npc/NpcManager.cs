using ModelLib;
using System;
using System.Collections.Generic;

namespace GameLabo
{
    /// <summary>
    /// ステージごとのNPC管理クラス
    /// </summary>
    public class NpcManager : IDisposable
    {
        /// <summary>
        /// ステージIDごとに対応するNPC管理クラスを保持
        /// Key   : StageID
        /// Value : そのステージ専用のNpcStageBase派生クラス
        /// </summary>
        public Dictionary<int, NpcStageBase> DicNPC;

        /// <summary>
        /// 現在のステージに属するNPCのModelInfo配列
        /// 外部からはこのプロパティ経由でNPC情報を取得・設定する
        /// </summary>
        public ModelInfo[] NpcInfo
        {
            get
            {
                // 現在のStageIDに対応するNPC情報を返す
                return DicNPC[StClass.StageID].NpcInfo;
            }
            set
            {
                // 現在のStageIDに対応するNPC情報を設定する
                DicNPC[StClass.StageID].NpcInfo = value;
            }
        }

        /// <summary>
        /// コンストラクタ
        /// ステージごとのNPC管理クラスを登録する
        /// </summary>
        public NpcManager()
        {
            DicNPC = new Dictionary<int, NpcStageBase>
            {
                // ステージ0用NPC
                {0, new NpcStage000() },
                // ステージ1用NPC
                {1, new NpcStage001() },
                // ステージ2用NPC
                {2, new NpcStage002() },
            };
        }

        /// <summary>
        /// 全NPC管理クラスのリソース解放
        /// </summary>
        public void Dispose()
        {
            foreach (var kvp in DicNPC)
            {
                kvp.Value.Dispose();
            }
        }

        /// <summary>
        /// 現在のステージのNPC初期化
        /// </summary>
        public void Init()
        {
            DicNPC[StClass.StageID].Init();
        }

        /// <summary>
        /// 現在のステージのNPC終了処理
        /// （ステージ切り替え時など）
        /// </summary>
        public void Term()
        {
            DicNPC[StClass.StageID].Term();
        }

        /// <summary>
        /// 現在のステージのNPCロジック更新
        /// （移動・AI・状態遷移など）
        /// </summary>
        public void Logic()
        {
            {
                DicNPC[StClass.StageID].Logic();
            }
        }

        /// <summary>
        /// 現在のステージのNPC描画
        /// </summary>
        public void Draw()
        {
            DicNPC[StClass.StageID].Draw();
        }
    }
}
