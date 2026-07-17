# VfdMeter

VfdMeterは、WindowsのCPU使用率、メモリ使用率、ネットワーク送受信速度を小型のVFD風オーバーレイで表示する、.NET 10 WinForms常駐アプリです。

## 現在の機能

- `CPU 023% MEM 048% NET ↓1.2M ↑80K` 形式でCPU・メモリ使用率とネットワーク速度を表示
- 1秒ごとに表示を更新
- 70%以上を橙色、90%以上を赤色で表示
- 枠なし、最前面、タスクバーに表示しない小型オーバーレイ
- 通知領域アイコンから表示、非表示、終了を操作
- Windows APIの `GetSystemTimes` と `GlobalMemoryStatusEx` を使用
- Windows APIの `GetIfTable2` で稼働中NICの送受信量を集計

DSKメーターは未実装です。ネットワーク速度の取得はOSの統計値を読み取るだけで、アプリ自身はネットワーク通信を行いません。また、管理者権限は必要ありません。

## 必要な環境

- Windows
- .NET 10 SDK

## ビルド

```powershell
dotnet restore --locked-mode
dotnet build
```

## 実行

```powershell
dotnet run --project src/VfdMeter/VfdMeter.csproj
```

起動するとオーバーレイと通知領域アイコンが表示されます。終了する場合は通知領域アイコンを右クリックし、`終了`を選択してください。

## プロジェクト構成

- `VfdMeter.slnx`: ソリューション
- `src/VfdMeter/Program.cs`: エントリーポイント
- `src/VfdMeter/VfdApplicationContext.cs`: アプリとリソースのライフサイクル管理
- `src/VfdMeter/OverlayForm.cs`: オーバーレイの表示と描画
- `src/VfdMeter/SystemMonitor.cs`: CPU・メモリ使用率の取得
- `src/VfdMeter/NetworkMonitor.cs`: NICカウンターの差分と速度の計算
- `src/VfdMeter/NativeNetworkApi.cs`: ネットワーク統計取得用Windows API

## 依存関係

Microsoft公式以外のNuGetパッケージは使用していません。現在は追加のNuGetパッケージ自体を必要としません。
