using System;
using System.Collections.Generic;
using static DX;

namespace GameLabo
{
    public partial class Physics : IDisposable
    {
        // =========================
        // ポータル判定用ブロックパターン定義
        // =========================

        // 草＋木で作られたポータル枠パターン
        // [z, y, x] の3次元配列
        ushort[,,] grassWoodPattern = new ushort[,,]
        {
            {
                { Chunks.Block_Wood, Chunks.Block_Wood, Chunks.Block_Wood, Chunks.Block_Wood },
                { Chunks.Block_Wood, Chunks.Block_Grass, Chunks.Block_Grass, Chunks.Block_Wood },
                { Chunks.Block_Wood, Chunks.Block_Grass, Chunks.Block_Grass, Chunks.Block_Wood },
                { Chunks.Block_Wood, Chunks.Block_Grass, Chunks.Block_Grass, Chunks.Block_Wood },
                { Chunks.Block_Wood, Chunks.Block_Wood, Chunks.Block_Wood, Chunks.Block_Wood },
            }
        };

        // ネザー風（草が黒曜石に変換された）パターン
        ushort[,,] netherPattern = new ushort[,,]
        {
            {
                { Chunks.Block_Wood, Chunks.Block_Wood, Chunks.Block_Wood, Chunks.Block_Wood },
                { Chunks.Block_Wood, Chunks.Block_Obs, Chunks.Block_Obs, Chunks.Block_Wood },
                { Chunks.Block_Wood, Chunks.Block_Obs, Chunks.Block_Obs, Chunks.Block_Wood },
                { Chunks.Block_Wood, Chunks.Block_Obs, Chunks.Block_Obs, Chunks.Block_Wood },
                { Chunks.Block_Wood, Chunks.Block_Wood, Chunks.Block_Wood, Chunks.Block_Wood },
            }
        };

        // 温泉構造パターン（水＋レンガ）
        ushort[,,] onsenPattern = new ushort[,,]
        {
            {
                { Chunks.Block_RedBrick, Chunks.Block_RedBrick, Chunks.Block_RedBrick, Chunks.Block_RedBrick, Chunks.Block_RedBrick  },
            },
            {
                { Chunks.Block_RedBrick, Chunks.Block_Water, Chunks.Block_Water, Chunks.Block_Water, Chunks.Block_RedBrick  },
            },
            {
                { Chunks.Block_RedBrick, Chunks.Block_Water, Chunks.Block_Water, Chunks.Block_Water, Chunks.Block_RedBrick  },
            },
            {
                { Chunks.Block_RedBrick, Chunks.Block_Water, Chunks.Block_Water, Chunks.Block_Water, Chunks.Block_RedBrick  },
            },
            {
                { Chunks.Block_RedBrick, Chunks.Block_Water, Chunks.Block_Water, Chunks.Block_Water, Chunks.Block_RedBrick  },
            },
            {
                { Chunks.Block_RedBrick, Chunks.Block_RedBrick, Chunks.Block_RedBrick, Chunks.Block_RedBrick, Chunks.Block_RedBrick  },
            },
        };

        // =========================
        // 内部用構造体・キュー
        // =========================

        // マッチしたブロック情報（後で一括変換するため）
        private struct PortalInfo
        {
            public (int x, int y, int z) Pos;   // ワールド座標
            public ushort BlockId;              // 元のブロックID
        }

        // パターン一致時に変換対象ブロックを溜めておくキュー
        private Queue<PortalInfo> ConversionBlock = new Queue<PortalInfo>();

        // =========================
        // 温泉判定
        // =========================

        // VECTOR版（設置位置から温泉構造か判定）
        public bool MatchOnsen(VECTOR pos)
        {
            return MatchOnsen(((int)Math.Floor(pos.x), (int)Math.Floor(pos.y), (int)Math.Floor(pos.z)));
        }

        // int座標版 温泉判定
        private bool MatchOnsen((int x, int y, int z) placedPos)
        {
            return MatchPattern(placedPos, onsenPattern);
        }

        // =========================
        // ポータルON / OFF判定
        // =========================

        // ブロック設置時に呼ばれる（草→ネザー化チェック）
        public void CheckPortalBlockOn(VECTOR pos)
        {
            CheckPortalBlockOn(((int)Math.Floor(pos.x), (int)Math.Floor(pos.y), (int)Math.Floor(pos.z)));
        }

        public void CheckPortalBlockOn((int x, int y, int z) pos)
        {
            // 草＋木のポータルが完成していれば変換処理が走る
            if (MatchGrassWood(pos))
            {
                return;
            }
        }

        // ブロック破壊時など（ネザー→草へ戻すチェック）
        public void CheckPortalBlockOff(VECTOR pos)
        {
            CheckPortalBlockOff(((int)Math.Floor(pos.x), (int)Math.Floor(pos.y), (int)Math.Floor(pos.z)));
        }

        public void CheckPortalBlockOff((int x, int y, int z) pos)
        {
            if (MatchNether(pos))
            {
                return;
            }
        }

        // =========================
        // 各ポータル変換処理
        // =========================

        // 草＋木 → ネザー（草を黒曜石へ）
        private bool MatchGrassWood((int x, int y, int z) placedPos)
        {
            if (MatchPattern(placedPos, grassWoodPattern))
            {
                // マッチした全ブロックを変換
                while (ConversionBlock.Count > 0)
                {
                    PortalInfo info = ConversionBlock.Dequeue();
                    ushort newBlockId = info.BlockId;
                    if (info.BlockId == Chunks.Block_Grass)
                        newBlockId = Chunks.Block_Obs;
                    StClass.WRLD.EnqueueBlockPlacement(info.Pos, newBlockId);
                }
                return true;
            }
            return false;
        }

        // ネザー → 草へ戻す
        private bool MatchNether((int x, int y, int z) placedPos)
        {
            if (MatchPattern(placedPos, netherPattern))
            {
                while (ConversionBlock.Count > 0)
                {
                    PortalInfo info = ConversionBlock.Dequeue();
                    ushort newBlockId = info.BlockId;
                    if (info.BlockId == Chunks.Block_Obs)
                        newBlockId = Chunks.Block_Grass;
                    StClass.WRLD.EnqueueBlockPlacement(info.Pos, newBlockId);
                }
                return true;
            }
            return false;
        }

        // =========================
        // パターンマッチ本体
        // =========================

        // 回転（0°,90°,180°,270°）＋基準点を全探索して一致するか判定
        private bool MatchPattern((int x, int y, int z) placedPos, ushort[,,] portalPattern)
        {
            int depth = portalPattern.GetLength(0);
            int height = portalPattern.GetLength(1);
            int width = portalPattern.GetLength(2);

            for (int rot = 0; rot < 4; rot++)
            {
                for (int baseZ = 0; baseZ < depth; baseZ++)
                {
                    for (int baseY = 0; baseY < height; baseY++)
                    {
                        for (int baseX = 0; baseX < width; baseX++)
                        {
                            ConversionBlock.Clear();
                            if (TryMatchWithBase(placedPos, baseX, baseY, baseZ, rot, portalPattern))
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        // 指定基準点・回転で完全一致するか
        private bool TryMatchWithBase((int x, int y, int z) placedPos, int baseX, int baseY, int baseZ, int rotation, ushort[,,] portalPattern)
        {
            int depth = portalPattern.GetLength(0);
            int height = portalPattern.GetLength(1);
            int width = portalPattern.GetLength(2);

            // 基準点も回転させる
            int rBaseX, rBaseZ;
            RotateXZ(baseX, baseZ, rotation, width, depth, out rBaseX, out rBaseZ);

            var matched = new List<PortalInfo>();

            for (int dz = 0; dz < depth; dz++)
            {
                for (int dy = 0; dy < height; dy++)
                {
                    for (int dx = 0; dx < width; dx++)
                    {
                        int rx, rz;
                        RotateXZ(dx, dz, rotation, width, depth, out rx, out rz);

                        int cx = placedPos.x + (rx - rBaseX);
                        int cy = placedPos.y + (dy - baseY);
                        int cz = placedPos.z + (rz - rBaseZ);

                        ushort need = portalPattern[dz, dy, dx];
                        ushort block = StClass.WRLD.GetWorldBlockId((cx, cy, cz));

                        if (block != need)
                            return false;

                        matched.Add(new PortalInfo { Pos = (cx, cy, cz), BlockId = block });
                    }
                }
            }

            // 全一致 → 変換キューに登録
            foreach (var p in matched) ConversionBlock.Enqueue(p);

            return true;
        }

        // =========================
        // XZ平面回転処理
        // =========================

        // rotation: 0=0°, 1=90°, 2=180°, 3=270°
        private void RotateXZ(int x, int z, int rotation, int width, int depth, out int rx, out int rz)
        {
            switch (rotation)
            {
                case 0: rx = x; rz = z; break;
                case 1: rx = z; rz = width - 1 - x; break;
                case 2: rx = width - 1 - x; rz = depth - 1 - z; break;
                case 3: rx = depth - 1 - z; rz = x; break;
                default: rx = x; rz = z; break;
            }
        }
    }
}
