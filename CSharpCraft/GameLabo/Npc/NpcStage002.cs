using ModelLib;
using System.Collections.Generic;
using static DX;
using static ModelLib.Model;

namespace GameLabo
{
    /// <summary>
    /// ステージ002用 NPC制御クラス
    /// ・固定配置NPC（かわうそ / カピバラ）
    /// ・簡易霧（フォグ）演出
    /// ・小さな水場チャンク生成
    /// </summary>
    public class NpcStage002 : NpcStageBase
    {
        /// <summary>
        /// 霧（フォグ）用の簡易パーティクル位置リスト
        /// x,y : 発生位置
        /// z   : 時間経過による進行量
        /// </summary>
        private List<VECTOR> fog;

        public NpcStage002()
        {
            // NPCは2体固定
            NpcInfo = new ModelInfo[2];

            fog = new List<VECTOR>();
        }

        /// <summary>
        /// リソース解放
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            for (int i = 0; i < NpcInfo.Length; i++)
            {
                NpcInfo[i]?.Dispose();
                NpcInfo[i] = null;
            }
            NpcInfo = null;
        }

        /// <summary>
        /// ステージ初期化
        /// ・NPC生成
        /// ・水場用チャンク生成
        /// </summary>
        public override void Init()
        {
            base.Init();


            {
                // かわうそ（座りモーション）
                ModelInfo npc = new ModelInfo();
                npc.Handle = -1;
                npc.Scale = VGet(0.01f, 0.01f, 0.01f);
                npc.Rotation = VGet(0, 0, 0);
                npc.Position = VGet(0, 0, 0);
                npc.ModelType = (int)Model_CharaType.Usochan;
                npc.DressUpTop = -1;
                npc.DressUpUpper = 0;
                npc.DressUpLower = -1;
                npc.UserName = "かわうそ";
                // モデル複製・設定
                npc.DuplicateModel(StClass.DAT.model.BaseModel[npc.ModelType].Handle, npc.ModelType);
                StClass.DAT.model.SetScale(ref npc, npc.Scale, StClass.DAT.model.BaseModel[npc.ModelType]);
                StClass.DAT.model.SetRotationXYZDegree(npc);
                StClass.DAT.model.SetPosition(npc);
                // 座りアニメーション
                npc.AnimeIndex = Model.C019_Sit;
                NpcInfo[0] = npc;
            }

            {
                // カピバラ（座りモーション）
                ModelInfo npc = new ModelInfo();
                npc.Handle = -1;
                npc.Scale = VGet(0.04f, 0.04f, 0.04f);
                npc.Rotation = VGet(0, 20, 0);
                npc.Position = VGet(0, 0, 0);
                npc.ModelType = (int)Model_CharaType.Capybara;
                npc.DressUpTop = -1;
                npc.DressUpUpper = 0;
                npc.DressUpLower = -1;
                npc.UserName = "カピバラ";
                npc.DuplicateModel(StClass.DAT.model.BaseModel[npc.ModelType].Handle, npc.ModelType);
                // モデル複製・設定
                StClass.DAT.model.SetScale(ref npc, npc.Scale, StClass.DAT.model.BaseModel[npc.ModelType]);
                StClass.DAT.model.SetRotationXYZDegree(npc);
                StClass.DAT.model.SetPosition(npc);
                // 座りアニメーション
                npc.AnimeIndex = 2;
                NpcInfo[1] = npc;
            }

            // 水場チャンク生成

            // 1チャンク分の簡易マップを手動生成
            Chunks.Chunk chunk = new Chunks.Chunk();
            chunk.blockIds = new ushort[Chunks.blockX, Chunks.blockY, Chunks.blockZ];
            for (int z = 0; z < Chunks.blockZ; z++)
            {
                for (int y = 0; y < Chunks.blockY; y++)
                {
                    for (int x = 0; x < Chunks.blockX; x++)
                    {
                        ushort u;

                        // 地面レベルのみブロック配置
                        if (y == 0)
                        {
                            // 奥側は空
                            if (z > 3)
                            {
                                u = Chunks.Block_Air;
                            }
                            // 枠をレンガで囲う
                            else if (x == 0)
                            {
                                u = Chunks.Block_RedBrick;
                            }
                            else if (x == 15)
                            {
                                u = Chunks.Block_RedBrick;
                            }
                            else if (z == 0)
                            {
                                u = Chunks.Block_RedBrick;
                            }
                            else if (z == 3)
                            {
                                u = Chunks.Block_RedBrick;
                            }
                            // 中央は水
                            else
                            {
                                u = Chunks.Block_Water;
                            }
                        }
                        else
                        {
                            u = Chunks.Block_Air;
                        }
                        chunk.blockIds[x, y, z] = u;
                    }
                }
            }

            // ワールドに登録
            StClass.WRLD.chunks.dicChunks[(0, 0, 0)] = chunk;
            StClass.WRLD.chunks.CreateVertex((0, 0, 0));
        }

        /// <summary>
        /// ステージ終了処理
        /// </summary>
        public override void Term()
        {
            base.Term();

            for (int i = 0; i < NpcInfo.Length; i++)
            {
                NpcInfo[i]?.Dispose();
                NpcInfo[i] = null;
            }
        }

        /// <summary>
        /// ロジック更新
        /// ・霧の生成／移動
        /// ・NPCアニメーション更新
        /// </summary>
        public override void Logic()
        {
            base.Logic();

            // 霧生成（確率）
            if (DX.GetRand(100) < 10)
            {
                VECTOR v = VGet(GetRand(16), GetRand(2), 0);
                fog.Add(v);
            }

            // 霧更新
            for (int i = 0; i < fog.Count; i++)
            {
                VECTOR v = fog[i];
                v.z += StClass.loopTime;
                fog[i] = v;

                // 一定距離進んだら消す
                if (v.z > 1.5)
                {
                    fog.RemoveAt(i);
                    i--;
                }
            }

            // NPCアニメーション更新
            for (int i = 0; i < NpcInfo.Length; i++)
            {
                if (NpcInfo[i] != null)
                {
                    ModelInfo m = NpcInfo[i];
                    StClass.DAT.model.PlayAnimeModel(ref m, StClass.loopTime, false);
                    NpcInfo[i] = m;
                }
            }
        }

        /// <summary>
        /// 描画処理
        /// ・NPC描画
        /// ・霧ビルボード描画
        /// </summary>
        public override void Draw()
        {
            base.Draw();

            // NPC描画
            for (int i = 0; i < NpcInfo.Length; i++)
            {
                if (NpcInfo[i] != null)
                {
                    // カピバラの位置
                    if (NpcInfo[i].ModelType == (int)Model_CharaType.Capybara)
                    {
                        StClass.DAT.model.DrawBase(NpcInfo[i], VGet(9, 0, 2));
                    }
                    // かわうその位置
                    else
                    {
                        StClass.DAT.model.Draw(NpcInfo[i], VGet(7, 0, 2));
                    }
                }
            }

            // 霧描画（ビルボード）
            foreach (var v in fog)
            {
                VECTOR vec = VGet(0, v.y, v.x);

                DrawBillboard3D(VGet(v.x, v.y + 1.5f, 0),
                    0.5f, 0.5f,
                    v.z,
                    0.0f,
                    StClass.DAT.FogHandle, TRUE);
            }
        }
    }
}
