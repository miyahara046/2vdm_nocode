# 2vdm-spec-generator

## 概要

**2vdm-spec-generator**は、制限を加えた画面遷移システムの自然言語仕様からVDM++記述を自動生成するツールです。Markdown形式で記述された画面遷移仕様を視覚的に編集・管理し、VDM++による形式仕様に自動変換することで、仕様の正確性と一貫性を保証します。

![スクリーンショット 2026-03-26](https://github.com/user-attachments/assets/8b236b0f-4a80-40ce-b68d-fd73fb3d6488)

## 目次

- [概要](#概要)
- [機能](#機能)
- [プロジェクト構造](#プロジェクト構造)
- [技術スタック](#技術スタック)
- [システム要件](#システム要件)
- [インストール方法](#インストール方法)
- [使用方法](#使用方法)
- [ライセンス](#ライセンス)

## 機能

### コア機能
- **フォルダ管理**: 仕様ファイルが含まれるフォルダを選択・管理
- **ファイル操作**: 
  - Markdown (.md) およびVDM++ (.vdmpp) ファイルの読み込み
  - ツリービュー形式での階層表示
  - ファイルの新規作成・削除
- **エディタ機能**:
  - Markdown形式での仕様編集
  - VDM++記述の編集・表示
  - リアルタイム構文処理
  - 行番号表示付きのエディタ
- **仕様変換**: Markdown形式をVDM++記述に自動変換
- **UI調整**:
  - フォントサイズの拡大・縮小・リセット
  - 画面レイアウトのカスタマイズ
- **リアルタイムファイル監視**: フォルダ内のファイル変更を自動検知

## プロジェクト構造

```
2vdm-spec-generator/
├── 2vdm-spec-generator.csproj    # プロジェクト設定ファイル
├── App.xaml                      # アプリケーション定義
├── App.xaml.cs
├── AppShell.xaml                 # シェル（ナビゲーション定義）
├── AppShell.xaml.cs
├── MainPage.xaml                 # メインページUI
├── MainPage.xaml.cs
├── MauiProgram.cs                # MAUI初期化設定
├── NoCodePage.xaml               # NoCodeページUI
├── NoCodePage.xaml.cs
├── StartPage.xaml                # スタートページUI
├── StartPage.xaml.cs
├── StartPageViewModel.cs          # スタートページのビジネスロジック
│
├── Controls/                       # カスタムUIコントロール
│   ├── LineNumberEditor.xaml      # 行番号付きテキストエディタ
│   ├── LineNumberEditor.xaml.cs
│   ├── TreeView.xaml              # カスタムツリービュー
│   └── TreeView.xaml.cs
│
├── Converters/                     # XAML値コンバーター
│   ├── IconConverter.cs           # アイコン表示用コンバーター
│   ├── IndentConverter.cs         # インデント処理用
│   ├── InverseBooleanConverter.cs # 真偽値反転用
│   ├── ListConverter.cs           # リスト変換用
│   ├── ScreenTypeToVisibilityConverter.cs  # 画面タイプの表示制御
│   └── SelectedToColorConverter.cs # 選択状態の色変換
│
├── Models/                         # データモデル
│   ├── FileSystemItem.cs          # ファイルシステムアイテム
│   ├── FolderItem.cs              # フォルダ情報
│   └── MarkdownFile.cs            # Markdownファイルモデル
│
├── Platforms/                      # プラットフォーム固有コード
│   ├── Android/                   # Android用実装
│   │   ├── AndroidManifest.xml
│   │   ├── MainActivity.cs
│   │   ├── MainApplication.cs
│   │   └── Resources/
│   ├── iOS/                       # iOS用実装
│   │   ├── AppDelegate.cs
│   │   ├── Info.plist
│   │   ├── Program.cs
│   │   └── Resources/
│   ├── MacCatalyst/               # Mac Catalyst用実装
│   │   ├── AppDelegate.cs
│   │   ├── Entitlements.plist
│   │   ├── Info.plist
│   │   └── Program.cs
│   ├── Tizen/                     # Tizen用実装
│   │   ├── Main.cs
│   │   └── tizen-manifest.xml
│   └── Windows/                   # Windows用実装
│       ├── app.manifest
│       ├── App.xaml
│       ├── App.xaml.cs
│       └── Package.appxmanifest
│
├── Properties/                     # プロジェクトプロパティ
│   └── launchSettings.json        # 起動設定
│
├── Resources/                      # アプリケーションリソース
│   ├── AppIcon/                   # アプリケーションアイコン
│   ├── Fonts/                     # カスタムフォント
│   ├── Images/                    # 画像リソース
│   ├── Raw/                       # 未処理リソース
│   ├── Splash/                    # スプラッシュスクリーン
│   └── Styles/                    # UIスタイル定義
│       ├── Colors.xaml            # 色定義
│       └── Styles.xaml            # スタイルリソース
│
├── Services/                       # ビジネスロジック・サービス
│   ├── GuiPositionStore.cs        # GUI位置情報の管理
│   ├── MarkdownToUiConverter.cs   # MarkdownをUIに変換
│   ├── MarkdownToVdmConverter.cs  # MarkdownをVDM++に変換（コア機能）
│   ├── ProjectManagementService.cs # プロジェクト管理
│   ├── ScreenListService.cs       # 画面リスト管理
│   └── UiToMarkdownConverter.cs   # UIをMarkdownに変換
│
├── View/                           # カスタムビュー・レンダラー
│   ├── ConditionInPutPopup.xaml   # 条件入力用ポップアップ
│   ├── ConditionInPutPopup.xaml.cs
│   ├── GuiDiagramDrawable.cs      # GUI図形描画クラス
│   ├── GuiDiagramRenderer.cs      # GUI図形レンダラー
│   ├── TimeoutPopup.xaml          # タイムアウト用ポップアップ
│   └── TimeoutPopup.xaml.cs
│
└── ViewModel/                      # MVVM ViewModel
    ├── GuiElement.cs              # GUI要素のVM
    ├── MainViewModel.cs           # メイン画面のVM
    └── NoCodePageViewModel.cs      # NoCodeページのVM
```

## 技術スタック

- **フレームワーク**: [.NET MAUI](https://dotnet.microsoft.com/en-us/apps/maui) - クロスプラットフォームUI開発
- **UIツールキット**: [CommunityToolkit.Maui](https://learn.microsoft.com/en-us/communitytoolkit/maui/) - 拡張UIコンポーネント
- **Markdown処理**: [Markdig](https://github.com/lunet-io/markdig) - 高速Markdownパーサー・レンダラー
- **対応プラットフォーム**: Windows、Android、iOS、macOS、Tizen

## システム要件

### 開発環境
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) 以上
- [Visual Studio 2022/2025](https://visualstudio.microsoft.com/) または [Visual Studio Code](https://code.visualstudio.com/)
- Windows 10 (ビルド 19041) 以上（Windows実行時）

### 実行環境
- **Windows**: Windows 10 (ビルド 19041) 以上
- **Android**: Android 5.0 (API 21) 以上

## インストール方法

### リポジトリのクローン

```bash
git clone https://github.com/yourusername/2vdm-spec-generator.git
cd 2vdm-spec-generator/2vdm-spec-generator
```

### 依存関係の解決とビルド

#### Visual Studioを使用する場合
1. `2vdm-spec-generator.sln` をVisual Studioで開く
2. ソリューションをビルド（ビルド > ソリューションのビルド）
3. 起動プロジェクトを設定して実行

#### コマンドラインでビルドする場合

```bash
# 依存関係の復元
dotnet restore

# プロジェクトのビルド
dotnet build

# Windows実行ファイルの生成
dotnet build -f net9.0-windows10.0.19041.0

# Android APKの生成
dotnet build -f net9.0-android -c Release
```

## 使用方法

### 基本的な操作フロー

1. **アプリケーション起動**
   - アプリケーションを実行します

2. **仕様フォルダの選択**
   - 「フォルダを選択」ボタンをクリック
   - Markdown仕様ファイルが含まれるフォルダを選択

3. **ファイル操作**
   - ツリービューに表示されたファイルを閲覧
   - 「新しいMarkdownファイルを作成」で新規ファイルを追加
   - 既存ファイルを選択して編集

4. **仕様の編集**
   - Markdownテキストエディタで仕様を入力
   - 画面遷移、条件、業務ロジックを定義

5. **VDM++への変換**
   - 対象のMarkdownファイルを選択
   - 「VDM++に変換」ボタンをクリック
   - 生成されたVDM++記述を確認

6. **ファイルの保存**
   - 「ファイルの変更を保存」でMarkdown編集内容を保存
   - 「VDM++記述を保存」で変換結果を保存

### その他の機能

- **フォントサイズ調整**: エディタメニューでフォントサイズを拡大・縮小
- **リアルタイム監視**: フォルダ内のファイル変更は自動検知
- **ファイル削除**: ツリービューから不要なファイルを削除

## プロジェクト開発について

### 主要な処理フロー

1. **Markdown解析**: Markdigを使用してMarkdownを構文木に変換
2. **VDM++生成**: では構文木をVDM++クラス定義に変換
3. **UI更新**: WPF/XAML画面に結果を反映

### サービスの説明

| サービス | 役割 |
|---------|------|
| `MarkdownToVdmConverter` | Markdown → VDM++への変換処理 |
| `MarkdownToUiConverter` | Markdown → UI表現への変換 |
| `ProjectManagementService` | プロジェクトファイル管理 |
| `ScreenListService` | 画面定義の管理・取得 |
| `GuiPositionStore` | UI配置情報の保存・復元 |

## ライセンス

このプロジェクトはBSD 2-Clause Licenseの下で公開されています。詳細については、[LICENSE](LICENSE)ファイルを参照してください。
