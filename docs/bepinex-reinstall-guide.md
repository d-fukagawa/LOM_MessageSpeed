# LOM_MessageSpeed向け BepInEx 6の導入・入れ直し手順

この手順は、ConfigEditorに「BepInExは見つかりませんでした」または「BepInExのファイルを確認できません」と表示された場合や、LOM_MessageSpeedが正常に動作せずBepInExを入れ直したい場合に使用してください。

ここでは、現在のファイルをすぐに削除しません。ゲームフォルダ外へバックアップしてから、動作確認済みのBepInExを導入します。BepInExのフォルダはセーブデータではありません。この手順でセーブデータを移動または削除する必要はありません。

## 1. ゲームを完全に終了する

『活俠傳（Legend of Mortal）』を終了します。Steamで「停止中」になったことも確認してください。ConfigEditorを開いている場合は、そのままで構いません。

## 2. ゲームフォルダを開く

Steamのライブラリで『活俠傳』を右クリックし、「プロパティ」→「インストール済みファイル」→「参照」の順に開きます。`Mortal.exe`が見えるフォルダがゲームフォルダです。

## 3. 現在のBepInExをバックアップする

デスクトップなど、ゲームフォルダの外に新しいバックアップフォルダを作ります。日付を付けた名前にすると区別しやすくなります。

```text
BepInEx-backup-YYYY-MM-DD
```

ゲームフォルダに次の項目がある場合は、削除せず、作成したバックアップフォルダへ移動します。

```text
BepInEx
doorstop_config.ini
winhttp.dll
.doorstop_version
changelog.txt
```

存在しない項目があっても問題ありません。`Mortal.exe`や`Mortal_Data`は移動しません。

移動後、バックアップ内の`BepInEx\plugins`と`BepInEx\config`を確認してください。以前から使っている他のPluginや設定がある場合、ここに残っています。バックアップは、新しい構成で動作確認できるまで残してください。

## 4. 動作確認済みBepInExを公式配布元から入手する

[BepInEx公式ビルド一覧](https://builds.bepinex.dev/projects/bepinex_be)を開き、build `#692`の次のファイルを選びます。

```text
BepInEx-Unity.Mono-win-x64-6.0.0-be.692+851521c.zip
```

直接リンクを使用する場合も、配布元が`https://builds.bepinex.dev/`であることを確認してください。

[動作確認済みZIP（BepInEx公式配布元）](https://builds.bepinex.dev/projects/bepinex_be/692/BepInEx-Unity.Mono-win-x64-6.0.0-be.692%2B851521c.zip)

ダウンロードしたZIPのSHA-256は次の値です。

```text
9A3472F5EEFB35A84AE8C6DEA16814B728AFF807C67C14FBFD448E20112951A6
```

PowerShellで確認する場合は、`<ZIPのパス>`を実際のファイルに置き換えて実行します。

```powershell
Get-FileHash -Algorithm SHA256 -LiteralPath '<ZIPのパス>'
```

値が一致しない場合は、そのZIPを展開せず、BepInEx公式ビルド一覧からもう一度入手してください。値が違うという情報だけで、原因や危険性を断定することはできません。

## 5. ZIPをゲームフォルダへ展開する

ZIPを開き、中のすべての項目を`Mortal.exe`と同じゲームフォルダへ展開します。

```text
<ゲームフォルダ>\Mortal.exe
<ゲームフォルダ>\doorstop_config.ini
<ゲームフォルダ>\winhttp.dll
<ゲームフォルダ>\BepInEx\core\BepInEx.Core.dll
```

`Mortal_Data\Managed`の中には展開しません。管理者として実行する必要はありません。

## 6. BepInExの初回準備を完了する

ゲームを一度起動し、タイトル画面まで進んだら終了します。終了後、ゲームフォルダに`BepInEx\config\BepInEx.cfg`と`BepInEx\LogOutput.log`が作成されていることを確認します。

## 7. LOM_MessageSpeedをインストールする

1. ConfigEditorの「BepInEx導入サポート」を開く
2. 「状態を再確認」を押す
3. 「プラグインの操作へ進む」を押す
4. 「プラグインをインストール」を押す

ConfigEditorはBepInExをダウンロード、展開、削除、修復しません。また、このGistの本文もダウンロードしません。

## 8. 必要な他Pluginや設定だけを戻す

まず、LOM_MessageSpeedだけでゲームを起動し、正常に動作することを確認してください。

以前の他Pluginや個別設定が必要な場合だけ、バックアップの`BepInEx\plugins`や`BepInEx\config`から一つずつ戻し、その都度ゲームを確認します。バックアップの`BepInEx`フォルダ全体を新しいフォルダへ上書きしないでください。

## 元の状態へ戻す

新しい構成で問題が起きた場合は、次の順で戻せます。

1. ゲームを完全に終了する
2. 新しく導入した`BepInEx`、`doorstop_config.ini`、`winhttp.dll`、`.doorstop_version`、`changelog.txt`を、別の一時フォルダへ移動する
3. 手順3で保存したバックアップの項目を、`Mortal.exe`があるゲームフォルダへ戻す
4. ゲームを起動して元の状態を確認する

新しく導入した項目も、確認が終わるまでは削除せずに移動してください。元のPluginやcfgを誤って失わずに比較できます。

## それでも動作しない場合

問題報告には、ConfigEditorの「問題報告用の詳細情報」と`BepInEx\LogOutput.log`が役立ちます。送信前に、ユーザー名などの個人情報を含むパスがないか確認してください。

参考: [BepInEx公式Unity Mono導入ガイド](https://docs.bepinex.dev/master/articles/user_guide/installation/unity_mono.html?tabs=tabid-win)
