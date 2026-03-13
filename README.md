# Spout Overlay Viewer

`Spout Overlay Viewer` は、Windows 上で Spout sender の映像を受信し、透過付きの最前面オーバーレイとして表示する `.NET 9` / Windows Forms アプリです。

VTube Studio や VSeeFace、VRChat などから送られた Spout テクスチャを、デスクトップ上にオーバーレイ表示する用途を想定しています。クリック透過にも対応しているため、他のアプリの操作を邪魔しにくい構成です。

## Features

- Spout sender の一覧取得と受信
- 透過付きオーバーレイの最前面表示
- 調整モードでの移動、リサイズ、アスペクト比維持
- クリック透過のパススルーモード
- トレイメニューからの sender 切り替え、表示切り替え、終了
- 表示位置、モード、透明度、ホットキーなどの設定保存


## Install

### GitHub Releases から入手する

1. [Releases ページ](https://github.com/tatsu020/spout-overlay-viewer/releases) を開きます。
2. いちばん新しいリリースを開きます。
3. `Assets` の中にある `spout-overlay-viewer-v0.1.0-win-x64.zip` をクリックしてダウンロードします。
4. ダウンロードした zip ファイルを右クリックして、`すべて展開` を選びます。
5. 展開したフォルダを開き、`SpoutOverlay.App.exe` をダブルクリックして起動します。

初回起動時に Windows の警告が出た場合は、`詳細情報` を押してから `実行` を選んでください。

## Usage

1. Spout sender を出力するアプリを起動します。
2. `Spout Overlay Viewer` を起動します。
3. タスクトレイのアイコンから sender を選択します。
4. 調整モードで位置やサイズを整えます。
5. 必要に応じてクリック透過モードへ切り替えます。


## 開発者向け

### Requirements

- Windows 10 / 11 x64
- `.NET 9 SDK`
- Spout sender を出力するアプリケーション

### Project Structure

```text
src/SpoutOverlay.App      アプリ本体
tests/SpoutOverlay.Tests  xUnit テスト
```

### Build

```powershell
dotnet restore .\SpoutOverlayViewer.sln
dotnet build .\SpoutOverlayViewer.sln
```

### Run

```powershell
dotnet run --project .\src\SpoutOverlay.App\SpoutOverlay.App.csproj
```

### Test

```powershell
dotnet test .\SpoutOverlayViewer.sln
```


### Settings

設定は次のファイルに保存されます。

```text
%AppData%\SpoutOverlay\settings.json
```

保存対象には、ウィンドウ位置、選択中 sender、表示モード、透明度、表示切り替えホットキーが含まれます。

### Tech Stack

- .NET 9
- Windows Forms
- `SpoutDx.Net.Interop`
- `Vortice.Direct3D11`
- `Vortice.Direct2D1`
- `Vortice.DirectComposition`

## Notes

- Spout sender 側はアルファ付きテクスチャを前提にしています。
- 表示切り替えホットキーは実行中のアプリから変更できます。
- 使用中のホットキーが他アプリと競合している場合は登録に失敗します。
