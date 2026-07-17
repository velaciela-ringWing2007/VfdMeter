# VfdMeter

VfdMeterは、WindowsのCPU使用率、メモリ使用率、ネットワーク送受信速度、ディスクアクティブ率を小型のVFD風オーバーレイで表示する、.NET 10 WinForms常駐アプリです。

## 現在の機能

- `CPU 023% MEM 048% NET ↓001.2M ↑080.0K DSK 014%` の固定形式で表示
- 1秒ごとに表示を更新
- 70%以上を橙色、90%以上を赤色で表示
- 枠なし、最前面、タスクバーに表示しない小型オーバーレイ
- オーバーレイ全面の左ドラッグによる位置移動
- DPIとタスクバーを考慮した初期配置および画面内補正
- 独立したTopMostウィンドウとしてタスクバー領域上へ配置
- 通知領域アイコンから表示、非表示、終了を操作
- オーバーレイの右クリックから通知領域アイコンと同じメニューを操作
- Windows APIの `GetSystemTimes` と `GlobalMemoryStatusEx` を使用
- Windows APIの `GetIfTable2` で稼働中NICの送受信量を集計
- Windows PerformanceCounterでディスクアクティブ率を取得

ネットワーク速度の取得はOSの統計値を読み取るだけで、アプリ自身はネットワーク通信を行いません。また、管理者権限は必要ありません。

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
- `src/VfdMeter/DiskMonitor.cs`: ディスクアクティブ率の取得
- `src/VfdMeter/MetricFormatter.cs`: メトリクスの固定幅表示

## 依存関係

Microsoft公式以外のNuGetパッケージは使用していません。ディスク統計取得にはMicrosoft公式の `System.Diagnostics.PerformanceCounter` を使用します。
