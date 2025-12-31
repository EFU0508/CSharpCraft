using GameLabo;
using System;
using System.Collections.Generic;
using static DX;

namespace Scenario
{
    /// <summary>
    /// シーン管理クラス
    /// ・ロード画面表示
    /// ・ステージ生成／切り替え
    /// ・ゲーム全体の進行管理
    /// </summary>
    public class Scene : IDisposable
    {
        /// <summary>
        /// ステージID → ステージマネージャ生成関数の対応表
        /// Func を使うことで「必要になった時点で生成」する
        /// </summary>
        private Dictionary<int, Func<BaseManager>> DicStage;

        /// <summary>
        /// コンストラクタ
        /// ステージ一覧を登録
        /// </summary>
        public Scene()
        {
            DicStage = new Dictionary<int, Func<BaseManager>>
            {
                // StageID と実際のステージクラスの対応
                {0, () => new Stage000.StageManager() },
                {1, () => new Stage001.StageManager() },
                {2, () => new Stage002.StageManager() },
            };
        }

        /// <summary>
        /// 解放処理（現状は特に無し）
        /// </summary>
        public void Dispose()
        {

        }

        /// <summary>
        /// ゲーム全体の実行ループ
        /// </summary>
        public void Run()
        {
            // =====================================
            // ロード画面処理
            // =====================================
            using (LoadData data = new LoadData())
            {
                while (StClass.isRunning)
                {
                    // Windows メッセージ処理
                    ProcessMessage();

                    // ロード処理更新
                    data.Update();

                    // ロード画面描画
                    data.Draw();

                    // 画面更新
                    ScreenFlip();

                    // ロード完了したら抜ける
                    if (data.isLoadData)
                    {
                        break;
                    }
                }
            }

            // ゲーム終了指示が出ていたらここで終了
            if (!StClass.isRunning)
            {
                return;
            }

            // =====================================
            // 初期シーン・ステージ設定
            // =====================================
            StClass.SceneID = 0;
            StClass.StageID = 0;
            StClass.EventID = 0;
            StClass.EventSubID = 0;
            StClass.NextSceneID = 0;
            StClass.NextStageID = 0;
            StClass.NextEventID = 0;
            StClass.NextEventSubID = 0;

            // =====================================
            // メインゲームループ
            // =====================================
            while (StClass.isRunning)
            {
                // ステージ内ループ継続フラグ
                StClass.isStayStage = true;

                // 次に遷移するシーン情報を現在値に反映
                StClass.SceneID = StClass.NextSceneID;
                StClass.StageID = StClass.NextStageID;
                StClass.EventID = StClass.NextEventID;
                StClass.EventSubID = StClass.NextEventSubID;

                // =================================
                // ステージ生成・実行
                // =================================
                // DicStage から該当ステージを生成し、処理を委譲
                using (BaseManager stageBase = DicStage[StClass.StageID]())
                {
                    stageBase.Run();
                }
                // Run() を抜けた時点でステージは破棄される
            }
        }
    }
}
