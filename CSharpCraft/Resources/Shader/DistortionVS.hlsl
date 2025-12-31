// =====================================================
// DxLib D3D11 標準：頂点シェーダ用 定数バッファ構造体
// DxLib が内部で毎フレーム設定してくれる
// =====================================================
struct DX_D3D11_VS_CONST_BUFFER_BASE
{
    // 画面座標変換用（2D描画向け）
    // 3D描画では基本的に未使用
    float4 AntiViewportMatrix[4];
    
    // 射影行列（View → Clip）
    // Perspective / Ortho のどちらかが入る
    float4 ProjectionMatrix[4];
    
    // ビュー行列（World → View）
    // DxLib は 3 行のみ使用（w は固定）
    float4 ViewMatrix[3];
    
    // ローカル → ワールド変換行列
    float4 LocalWorldMatrix[3];

    // トゥーンアウトラインの太さ（DxLib用）
    float4 ToonOutLineSize;
    
    // ライティング設定（DxLib内部用）
    float DiffuseSource;
    float SpecularSource;
    float MulSpecularColor;
    
    // アライメント調整用パディング
    float Padding;
};

// =====================================================
// 定数バッファ本体
// register(b1) は DxLib が固定で使用
// =====================================================
cbuffer cbD3D11_CONST_BUFFER_VS_BASE : register(b1)
{
    DX_D3D11_VS_CONST_BUFFER_BASE g_Base;
};

// =====================================================
// 頂点シェーダ入力構造体
// DxLib の MV1 モデルから自動で渡される
// =====================================================
struct VS_INPUT
{
    float3 Position : POSITION0; // ローカル座標
    float4 SubPosition : POSITION1; // 追加頂点情報
    float3 Normal : NORMAL0; // 法線   
    float3 Tangent : TANGENT; // 接線
    float3 Binormal : BINORMAL0; // 従法線
    float4 DiffuseColor : COLOR0; // 頂点カラー
    float4 SpecularColor : COLOR1; // スペキュラカラー
    float2 TextureCoord0 : TEXCOORD0; // UV0
    float2 TextureCoord1 : TEXCOORD1; // UV1
};

// =====================================================
// 頂点シェーダ出力構造体
// ピクセルシェーダへ渡す
// =====================================================
struct VS_OUTPUT
{
    float4 pos : SV_POSITION; // クリップ空間座標
    float4 color : COLOR0; // 頂点カラー
    float2 uv : TEXCOORD0; // UV
};

// =====================================================
// 頂点シェーダ本体
// =====================================================
VS_OUTPUT main(VS_INPUT input)
{
    // -------------------------------------------------
    // UV 座標（今回はそのまま使用）
    // -------------------------------------------------
    float u = input.TextureCoord0.x;
    float v = input.TextureCoord0.y;

    // -------------------------------------------------
    // ローカル座標
    // モデル空間上の頂点位置
    // -------------------------------------------------
    float3 pos = input.Position;

    // 座標変換用ワーク変数
    float4 lLocalPosition;
    float4 lWorldPosition;
    float4 lViewPosition;

    // 法線用
    float3 lWorldNrm;
    float3 lViewNrm;

    // -------------------------------------------------
    // ローカル座標 → float4 化
    // -------------------------------------------------
    lLocalPosition.xyz = pos;
    lLocalPosition.w = 1.0f;

    // -------------------------------------------------
    // ローカル → ワールド変換
    // LocalWorldMatrix は DxLib が設定
    // -------------------------------------------------
    lWorldPosition.x = dot(lLocalPosition, g_Base.LocalWorldMatrix[0]);
    lWorldPosition.y = dot(lLocalPosition, g_Base.LocalWorldMatrix[1]);
    lWorldPosition.z = dot(lLocalPosition, g_Base.LocalWorldMatrix[2]);
    lWorldPosition.w = 1.0f;

    // -------------------------------------------------
    // ワールド → ビュー変換
    // -------------------------------------------------
    lViewPosition.x = dot(lWorldPosition, g_Base.ViewMatrix[0]);
    lViewPosition.y = dot(lWorldPosition, g_Base.ViewMatrix[1]);
    lViewPosition.z = dot(lWorldPosition, g_Base.ViewMatrix[2]);
    lViewPosition.w = 1.0f;

    VS_OUTPUT o;
    
    // -------------------------------------------------
    // ビュー → クリップ（射影変換）
    // -------------------------------------------------
    o.pos.x = dot(lViewPosition, g_Base.ProjectionMatrix[0]);
    o.pos.y = dot(lViewPosition, g_Base.ProjectionMatrix[1]);
    o.pos.z = dot(lViewPosition, g_Base.ProjectionMatrix[2]);
    o.pos.w = dot(lViewPosition, g_Base.ProjectionMatrix[3]);

    // -------------------------------------------------
    // 頂点カラーをそのまま出力
    // -------------------------------------------------
    o.color = input.DiffuseColor;

    // -------------------------------------------------
    // UV をそのまま出力
    // -------------------------------------------------
    o.uv = input.TextureCoord0;
    
    return o;
}
