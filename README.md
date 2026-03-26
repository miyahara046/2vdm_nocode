# 2vdm-spec-generator

## 概要

**2vdm-spec-generator**は、制限を加えた画面遷移システムの自然言語仕様からVDM++記述を自動生成するツールです。
![スクリーンショット 2026-03-26](https://github.com/user-attachments/assets/8b236b0f-4a80-40ce-b68d-fd73fb3d6488)

## 目次

- [概要](#概要)
- [機能](#機能)
- [プロジェクト構造](#プロジェクト構造)
- [使用技術](#使用技術)
- [インストール方法](#インストール方法)
- [使用方法](#使用方法)
- [ライセンス](#ライセンス)

## 機能

### 1. Markdownモード

Markdownモードでは、仕様ファイルを直接編集し、VDM++仕様へ変換できます。

- 仕様ファイルを含むフォルダの選択
- Markdownファイルの一覧表示
- Markdownファイルの新規作成
- 選択中ファイルの削除
- Markdownファイルの編集
- MarkdownからVDM++への変換
- 生成したVDM++の保存
- Markdownの保存
- フォントサイズの拡大、縮小、リセット
- フォルダ内ファイル変更の監視

### 2. NoCodeモード

NoCodeモードでは、Markdown仕様をGUI要素として扱いながら編集できます。

- 仕様ファイルを含むフォルダの選択
- Markdownファイルの一覧表示
- Markdownファイルの新規作成
- GUI上での画面要素の表示
- クラスの種類選択、追加
- 画面の追加
- ボタンの追加
- イベントの追加
- タイムアウトの追加
- クラス名（画面名）の変更
- GUI要素の削除
- GUI編集結果のVDM++への反映
- フォルダ内ファイル変更の監視

### 3. 起動モードの切り替え

アプリ起動時のスタートページから、次の2つのモードを選択できます。

- `Markdown`
- `NoCode`

## プロジェクト構造

本プロジェクトの主要な構成は次の通りです。

```
2vdm-spec-generator/ 
┣ 2vdm-spec-generator.csproj      # プロジェクト設定ファイル
┣ App.xaml                        # アプリケーション定義
┣ App.xaml.cs
┣ AppShell.xaml                   # シェル（ページ遷移ナビゲーション）
┣ AppShell.xaml.cs
┣ MainPage.xaml                   # MarkdownページUI
┣ MainPage.xaml.cs
┣ MauiProgram.cs                  # MAUI初期化設定
┣ NoCodePage.xaml                 # NoCodeページUI
┣ NoCodePage.xaml.cs
┣ StartPage.xaml                  # スタートページUI
┣ StartPage.xaml.cs
┣ StartPageViewModel.cs           # スタートページのロジック
┃
┣ Controls/                       # カスタムUIコントロール
┃   ┣ LineNumberEditor.xaml       # Markdownテキスト表示成型
┃   ┣ LineNumberEditor.xaml.cs
┃   ┣ TreeView.xaml               # ツリービュー表示成型
┃   ┗ TreeView.xaml.cs
┃
┣ Converters/                              # XAML値コンバーター
┃   ┣ IconConverter.cs                     # フォルダアイコン表示用
┃   ┣ IndentConverter.cs                   # フォルダ内インデント処理用
┃   ┣ InverseBooleanConverter.cs           # 真偽値反転用
┃   ┣ ListConverter.cs                     # ツリー展開用
┃   ┣ ScreenTypeToVisibilityConverter.cs   # 画面タイプの判定用
┃   ┗ SelectedToColorConverter.cs          # 選択状態の色変換用
┃
┣ Models/                         # データモデル
┃   ┣ FileSystemItem.cs           # ファイルシステムアイテム
┃   ┣ FolderItem.cs               # フォルダ情報
┃   ┗ MarkdownFile.cs             # Markdownファイルモデル
┃
┣ Platforms/                      # プラットフォーム固有コード
┃   ┣ Android/                    # Android用実装
┃   ┃   ┣ AndroidManifest.xml
┃   ┃   ┣ MainActivity.cs
┃   ┃   ┣ MainApplication.cs
┃   ┃   ┗ Resources/
┃   ┣ iOS/                        # iOS用実装
┃   ┃   ┣ AppDelegate.cs
┃   ┃   ┣ Info.plist
┃   ┃   ┣ Program.cs
┃   ┃   ┗ Resources/
┃   ┣ MacCatalyst/                # Mac Catalyst用実装
┃   ┃   ┣ AppDelegate.cs
┃   ┃   ┣ Entitlements.plist
┃   ┃   ┣ Info.plist
┃   ┃   ┗ Program.cs
┃   ┣ Tizen/                      # Tizen用実装
┃   ┃   ┣ Main.cs
┃   ┃   ┗ tizen-manifest.xml
┃   ┗ Windows/                    # Windows用実装
┃       ┣ app.manifest
┃       ┣ App.xaml
┃       ┣ App.xaml.cs
┃       ┗ Package.appxmanifest
┃
┣ Properties/                     # プロジェクトプロパティ
┃   ┗ launchSettings.json         # 起動設定
┃     
┣ Resources/                      # アプリケーションリソース
┃   ┣ AppIcon/                    # アプリケーションアイコン
┃   ┣ Fonts/                      # カスタムフォント
┃   ┣ Images/                     # 画像リソース
┃   ┣ Raw/                        # 未処理リソース
┃   ┣ Splash/                     # スプラッシュスクリーン
┃   ┗ Styles/                     # UIスタイル定義
┃       ┣ Colors.xaml             # 色定義
┃       ┗ Styles.xaml             # スタイルリソース
┃
┣ Services/                       # Modelの根幹部分
┃   ┣ GuiPositionStore.cs         # GUI位置情報の管理
┃   ┣ MarkdownToUiConverter.cs    # MarkdownをUIに変換
┃   ┣ MarkdownToVdmConverter.cs   # MarkdownをVDM++に変換
┃   ┣ ProjectManagementService.cs # プロジェクト管理
┃   ┣ ScreenListService.cs        # 画面リスト管理
┃   ┗ UiToMarkdownConverter.cs    # UIをMarkdownに変換
┃
┣ View/                           # カスタムビュー
┃   ┣ ConditionInPutPopup.xaml    # イベント入力用ポップアップ
┃   ┣ ConditionInPutPopup.xaml.cs
┃   ┣ GuiDiagramDrawable.cs       # GUI図形描画クラス
┃   ┣ GuiDiagramRenderer.cs       # GUI図形レンダラー
┃   ┣ TimeoutPopup.xaml           # タイムアウト入力用ポップアップ
┃   ┗ TimeoutPopup.xaml.cs
┃
┗ ViewModel/                      # ViewModel
    ┣ GuiElement.cs               # GUI要素のデータ
    ┣ MainViewModel.cs            # マークダウンページ用のVM
    ┗ NoCodePageViewModel.cs      # NoCodeページのVM
```

## 使用技術

- [.NET MAUI](https://dotnet.microsoft.com/en-us/apps/maui)
- [CommunityToolkit.Maui](https://learn.microsoft.com/en-us/communitytoolkit/maui/)
- [Markdig](https://github.com/lunet-io/markdig)

## インストール方法

### 開発環境
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) 以上
- [Visual Studio 2026](https://visualstudio.microsoft.com/) または 他の対応エディタ

### クローンとコマンドラインでのビルド（Visual Studioだったらボタン操作のみで可能）


1. リポジトリのクローン

    ```bash
    git clone https://github.com/yourusername/2vdm-spec-generator.git
    ```

2. プロジェクトディレクトリに移動

    ```bash
    cd 2vdm-spec-generator
    ```

3. 依存関係の復元

    ```bash
    dotnet restore
    ```

4. プロジェクトのビルド

    ```bash
    dotnet build
    ```


## 使用方法

### 起動
アプリを起動すると、スタートページで次のいずれかを選択できます。

```
 - Markdown
 - NoCode
```

### Markdownモード
1. **フォルダを選択**ボタンをクリックし、仕様ファイルが含まれるフォルダを選択します。
2. ツリービューに表示されたMarkdownファイルを開きます。
4. ファイルの内容を編集し、**ファイルの変更を保存**ボタンで保存します。
5. **VDM++に変換**ボタンをクリックすると、選択したMarkdownファイルがVDM++記述に変換されます。
6. 生成されたVDM++ファイルを **VDM++記述を保存** ボタンで保存します。

### NoCode モード
1. **フォルダを選択**ボタンをクリックし、仕様ファイルが含まれるフォルダを選択します。
2. ツリービューに表示されたMarkdownファイルを開きます。
3. GUI上で画面、ボタン、イベント、タイムアウトなどを編集します。

## ライセンス

このプロジェクトはBSD 2-Clause Licenseの下で公開されています。詳細については、[LICENSE](LICENSE)ファイルを参照してください。
