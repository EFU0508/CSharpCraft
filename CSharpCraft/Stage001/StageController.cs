using GameLabo;
using ModelLib;
using System.Linq;

namespace Stage001
{
    /// <summary>
    /// ステージ001専用の制御クラス
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
            // 基底クラス側の共通更新処理
            base.Update();

            // プレイヤー操作・移動・入力処理
            PlayerController();

            // 登録されている全モデルを更新
            foreach (int i in StClass.DAT.modelInfo.Keys.ToList())
            {
                // キーが存在し、かつモデル情報が null でない場合のみ処理
                if (StClass.DAT.modelInfo.ContainsKey(i) && (StClass.DAT.modelInfo[i] != null))
                {
                    // モデル情報を一旦ローカル変数にコピー
                    ModelInfo minfo = StClass.DAT.modelInfo[i];

                    // アニメーション再生（ループあり）
                    StClass.DAT.model.PlayAnimeModel(ref minfo, StClass.loopTime, true);

                    // 回転角度を反映
                    StClass.DAT.model.SetRotationXYZDegree(minfo);

                    // 座標を反映
                    StClass.DAT.model.SetPosition(minfo);

                    // 更新したモデル情報を辞書に戻す
                    StClass.DAT.modelInfo[i] = minfo;
                }
            }

            // ブロック設置処理（1フレーム分）
            StClass.WRLD.ProcessBlockPlacements(1);

            // プレイヤー位置を基準にチャンクの更新
            StClass.WRLD.UpdateChunks(StClass.DAT.modelInfo[StClass.UserID].Position);

            // プレイヤーが通常モード（Mode == 0）のときのみ照準更新
            if (StClass.DAT.modelInfo[StClass.UserID].Mode == 0)
            {
                StClass.WRLD.UpdateAim(StClass.DAT.modelInfo[StClass.UserID].Position, StClass.DAT.modelInfo[StClass.UserID].Height, 16.0f);
            }

            // NPC の思考・行動ロジック更新
            {
                StClass.NPC.Logic();
            }
        }
    }
}
