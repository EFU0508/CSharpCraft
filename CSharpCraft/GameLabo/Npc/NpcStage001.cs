using CmnDxlib;
using ModelLib;
using System;
using System.Collections.Generic;
using System.Linq;
using static DX;

namespace GameLabo
{
    /// <summary>
    /// ステージ001専用のNPC制御クラス
    /// カピバラNPCの生成・移動・当たり判定・復活を管理する
    /// </summary>
    public class NpcStage001 : NpcStageBase
    {
        /// <summary>
        /// ランダム処理用
        /// </summary>
        private static Random random = new Random();

        /// <summary>
        /// NPCごとの行動エリア定義
        /// [x, y, z, radius]
        /// </summary>
        public static readonly float[,] NpcIds =
        {
            { 31, 128, 76, 100 },
            { -31, 128, 76, 100 },
            { -31, 128, 49, 100 },
            { 31, 128, 49, 100 },
            { 31, 128, -76, 100 },
            { -31, 128, -76, 100 },
            { -31, 128, -49, 100 },
            { 31, 128, -49, 100 },
        };

        /// <summary>
        /// コンストラクタ
        /// NPC数 = 定義されているエリア数
        /// </summary>
        public NpcStage001()
        {
            NpcInfo = new ModelInfo[NpcIds.GetLength(0)];
        }

        /// <summary>
        /// NPCのリソース解放
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
        /// NPCのAI・移動・当たり判定などの更新処理
        /// </summary>
        public override void Logic()
        {
            base.Logic();

            int userid = StClass.UserID;

            // 初期化処理
            for (int i = 0; i < NpcInfo.Length; i++)
            {
                // 初回生成時のNPC初期化
                if (NpcInfo[i] == null)
                {
                    NpcInfo[i] = new ModelInfo();
                    NpcInfo[i].Handle = -1;
                    NpcInfo[i].Scale = VGet(0.015f, 0.015f, 0.015f);
                    NpcInfo[i].Rotation = VGet(0, 0, 0);
                    NpcInfo[i].ModelType = -1;
                    NpcInfo[i].DressUpTop = -1;
                    NpcInfo[i].DressUpUpper = 0;
                    NpcInfo[i].DressUpLower = -1;
                    NpcInfo[i].OldAnimeIndex = -1;
                    NpcInfo[i].HP = 0;
                    NpcInfo[i].UserName = "カピバラ";
                }

                // モデル生成
                ModelInfo npc = NpcInfo[i];

                if (userid == StClass.UserID)
                {
                    // モデルが未生成ならランダムに生成
                    if (npc.Handle < 0)
                    {
                        npc.ModelType = (int)Model.Model_CharaType.Capybara;
                        NpcInfo[i].DuplicateModel(StClass.DAT.model.BaseModel[npc.ModelType].Handle, npc.ModelType);
                        StClass.DAT.model.SetScale(ref NpcInfo[i], NpcInfo[i].Scale, StClass.DAT.model.BaseModel[npc.ModelType]);
                    }

                    npc.NewPosition = npc.Position;
                    npc.MoveTimer += StClass.loopTime;

                    // 生存中の挙動
                    if (npc.HP > 0)
                    {
                        // 一定時間ごとに移動ON/OFFを切り替える
                        if (npc.MoveTimer >= (1f + GetRand(15)))
                        {
                            npc.MoveTimer = 0f;
                            if (!npc.IsMoving)
                            {
                                SetRandomTarget(ref npc, NpcIds[i, 3]);
                            }
                            npc.IsMoving = !npc.IsMoving;
                        }

                        // アニメーション設定
                        npc.AnimeIndex = 0;

                        // 移動処理
                        if (npc.IsMoving)
                        {
                            npc.AnimeIndex = 1;
                            float dx = npc.TargetPos.x - npc.NewPosition.x;
                            float dz = npc.TargetPos.z - npc.NewPosition.z;
                            float dist = (float)Math.Sqrt(dx * dx + dz * dz);

                            // 目的地到達
                            if (dist < 1.0f)
                            {
                                SetRandomTarget(ref npc, NpcIds[i, 3]);
                            }
                            else
                            {
                                // 移動
                                float moveDist = 2f * StClass.loopTime;
                                float ratio = moveDist / dist;
                                if (ratio > 1) ratio = 1;
                                npc.NewPosition.x += dx * ratio;
                                npc.NewPosition.z += dz * ratio;

                                // 向き調整
                                float angleDeg = (float)(Math.Atan2(dx, dz) * 180.0 / Math.PI);
                                angleDeg += 180f;
                                angleDeg = (angleDeg + 360f) % 360f;
                                npc.Rotation.y = angleDeg;
                            }

                            // 行動範囲制限
                            VECTOR center = VGet(NpcIds[i, 0], NpcIds[i, 1], NpcIds[i, 2]);
                            float dxToCenter = npc.NewPosition.x - center.x;
                            float dzToCenter = npc.NewPosition.z - center.z;
                            float distSqToCenter = dxToCenter * dxToCenter + dzToCenter * dzToCenter;
                            float radius = NpcIds[i, 3];

                            if (distSqToCenter > radius * radius)
                            {
                                npc.IsMoving = false;
                            }
                            else
                            {
                                // 当たり判定＆重力
                                VECTOR oldPos = npc.Position;
                                VECTOR newPos = oldPos;
                                bool isWall = true;

                                // X方向
                                newPos.x = npc.NewPosition.x;
                                if (StClass.DAT.physics.IsColliding(newPos, 0.25f, 1.5f))
                                {
                                    newPos.x = oldPos.x;
                                }
                                else
                                {
                                    isWall = false;
                                }

                                // Z方向
                                newPos.z = npc.NewPosition.z;
                                if (StClass.DAT.physics.IsColliding(newPos, 0.25f, 1.5f))
                                {
                                    newPos.z = oldPos.z;
                                }
                                else
                                {
                                    isWall = false;
                                }
                                if (isWall)
                                {
                                    npc.NewPosition.y += 1.75f;
                                    if (!StClass.DAT.physics.IsColliding(npc.NewPosition, 0.25f, 1.5f))
                                    {
                                        newPos = npc.NewPosition;
                                    }
                                }
                                npc.Position = newPos;
                            }
                        }
                        {
                            // 重力処理
                            VECTOR nextPos = npc.Position;
                            nextPos.y -= gravity * StClass.loopTime;
                            float? groundY = StClass.DAT.physics.GetGroundY(npc.Position, nextPos, 0.25f);
                            if (groundY.HasValue)
                            {
                                npc.Position.y = groundY.Value;
                            }
                            else
                            {
                                npc.Position.y = nextPos.y;
                            }
                        	// 落下死
                            if (npc.Position.y < 0)
                            {
                                npc.HP = 0;
                                npc.MoveTimer = 0;
                                MV1DeleteModel(npc.Handle);
                                npc.Handle = -1;
                                npc.ModelType = -1;
                            }
                        }

                        // 温泉判定
                        if (StClass.WRLD.GetWorldBlockId(npc.Position) == Chunks.Block_Water)
                        {
                            // エンディングへ移行
                            if ((!StClass.Ending) && (StClass.DAT.physics.MatchOnsen(npc.Position)))
                            {
                                StClass.isStayStage = false;
                                StClass.NextStageID = 2;
                            }
                        }

                    }
                    // 死亡中（リスポーン）
                    else
                    {
                        if (npc.MoveTimer >= (10f + GetRand(15)))
                        {
                            double angle = random.NextDouble() * 2 * Math.PI;
                            double r = Math.Sqrt(random.NextDouble()) * NpcIds[i, 3];
                            float x = (NpcIds[i, 0] + (float)(Math.Sin(angle) * r));
                            float z = (NpcIds[i, 2] + (float)(Math.Cos(angle) * r));
                            VECTOR newPos = VGet(x, NpcIds[i, 1], z);
                            npc.Position = newPos;
                            npc.HP = 1;
                        }
                    }

                    {
                        // 矢との当たり判定
                        foreach (KeyValuePair<int, ModelInfo> info in StClass.DAT.modelInfo.ToList())
                        {
                            ModelInfo m = StClass.DAT.modelInfo[info.Key];
                            if (m == null)
                            {
                                continue;
                            }
                            if (m.ArrowAlive == TRUE)
                            {
                                if (npc.HP > 0)
                                {
                                    MV1RefreshCollInfo(m.ArrowHandle);
                                    MV1_COLL_RESULT_POLY_DIM hit = MV1CollCheck_Sphere(m.ArrowHandle, -1, npc.Position, NpcInfo[i].CapsuleR * 2f);
                                    if (hit.HitNum > 0)
                                    {
                                        m.Mode = 4000;
                                        m.HunterStatus = 2;
                                        m.ArrowAlive = FALSE;
                                        npc.HP = 0;
                                        npc.MoveTimer = 0;
                                        StClass.DAT.modelInfo[info.Key] = m;
                                        if (info.Key == StClass.UserID)
                                        {
                                            StopSoundMem(StClass.DAT.mp3_Mortality);
                                            PlaySoundMem(StClass.DAT.mp3_Mortality, DX.DX_PLAYTYPE_BACK);
                                        }
                                        MV1DeleteModel(npc.Handle);
                                        npc.Handle = -1;
                                        npc.ModelType = -1;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                NpcInfo[i] = npc;
            }
        }

        /// <summary>
        /// NPCの次の移動目標を設定
        /// プレイヤーを狙うか、ランダム移動
        /// </summary>
        private void SetRandomTarget(ref ModelInfo npc, float radius)
        {
            int lotResult = random.Next(0, StClass.DAT.modelInfo.Count - 1);
            int userid = StClass.DAT.modelInfo.ElementAt(lotResult).Key;

            if ((StClass.DAT.modelInfo[userid] != null) && (GetRand(100) < 20))
            {
                npc.TargetPos = StClass.DAT.modelInfo[userid].Position;
            }
            else
            {
                float degree = GetRand(360);
                float angle = Calc.DegreeToRadian(degree);
                double r = Math.Sqrt(random.NextDouble()) * radius;
                float tx = -(float)(Math.Sin(angle) * r);
                float tz = -(float)(Math.Cos(angle) * r);
                float centerX = npc.Position.x + tx;
                float centerZ = npc.Position.z + tz;
                npc.TargetPos = VGet(centerX, npc.Position.y, centerZ);
            }
        }

        /// <summary>
        /// NPC描画処理
        /// </summary>
        public override void Draw()
        {
            base.Draw();

            for (int i = 0; i < NpcInfo.Length; i++)
            {
                if ((NpcInfo[i] != null) && (NpcInfo[i].HP > 0))
                {
                    StClass.DAT.model.SetRotationXYZDegree(NpcInfo[i]);
                    StClass.DAT.model.PlayAnimeModel(ref NpcInfo[i], StClass.loopTime);
                    StClass.DAT.model.DrawBase(NpcInfo[i], VGet(0, 0, 0));
                }
            }
        }
    }
}
