using GameLabo;
using static DX;

namespace Stage002
{
    /// <summary>
    /// ステージ002専用の制御クラス
    /// プレイヤー・モデル・ワールド・NPC の更新を担当する
    /// </summary>
    public class StageController : BaseController
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public StageController()
        {

        }

        /// <summary>
        /// リソース解放処理
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
        }

        /// <summary>
        /// 毎フレーム呼ばれる更新処理
        /// </summary>
        public override void Update()
        {
            ClearDrawScreen();
            // 基底クラス側の共通更新処理
            base.Update();

            // NPC の思考・行動ロジック更新
            {
                StClass.NPC.Logic();
            }
        }
    }
}
