#  
![karashina_backgroupd](https://yt3.googleusercontent.com/SmDHp9rXpW5sj4BzIDj4SKruXZnn5w3JKdm8JWCE9-gpQbZ9KbwmfgYtC-6-bTpbHAk2G7iLsIg=w1707-fcrop64=1,00005a57ffffa5a8-k-c0xffffffff-no-nd-rj)
<h1 align="center">🖥️　AutoWeaponCalc (AWC)　🖥️</h1>

## 📜　概要
動画用にキャラの武器比較をつくるとき、いちいちgcsimのsubstat optimizerモードを何度も使って計算するのが面倒だったので、
一連の手順を自動化するために作ったコンソールアプリケーションです　<br>

`config.txt`の編成、ローテーションを使って、指定したキャラの武器ごとの個人DPSを計算し、csvに出力します　<br>

開発に関するご意見は[Issue](https://github.com/Karashina/AutoWeaponCalc/issues)にてお受けしております
<br>

## 🧑‍💻 つかいかた
### 📥　事前準備
1. gcsimの[Releases](https://github.com/genshinsim/gcsim/releases)より、`gcsim.exe`をダウンロードする[^1]
2. ダウンロードした`gcsim.exe`を`resource/execBinary`ディレクトリに配置する


[^1]:Windows以外の場合は別途案内(予定)
---
### 📊　実行方法
1. 計算を行いたいgcsimのアクションリストを`config.txt`として保存する<br>
    [参考](resource/input/example/config.txt)

1. 計算を行いたい聖遺物のセット効果の組み合わせを`artifacts.csv`に記述し、保存する <br>
    [参考](resource/input/example/artifacts.csv)

1. 上記の手順で行った`config.txt`と`artifacts.csv`を`resource/input`へ移動させる

1. run.batで起動

1. `mode selection([a]uto / [n]oartifact / [m]anual ) :`　<br>
モード設定です　<br>
"a"に設定するとこのあとの7に`0`を、8と9にそれぞれ`y`と`y`を入れて飛ばします　<br>
"n"に設定するとこのあとの7に`0`を、8と9にそれぞれ`n`と`y`を入れて飛ばします　<br>

1. `Type the name of the character to calculate:`　<br>
計算したいキャラクターの名前を入力してください　<br>
キャラ名はconfigファイルに記載のある通りに入力してください　<br>

1. `Type the refinement rank of the weapon to calculate [0=auto][1-5] :`　<br>
武器の精錬ランクを設定してください　<br>
0にすると⭐5はR1、⭐4はR5で計算されます　<br>
武器csvのrarityで管理しています(1=⭐5, 0=⭐4)　<br>

1. `Do you want to use artifact mode? [y|n]:`　<br>
聖遺物モードを使用するかの選択肢です　<br>
使用する場合`artifacts.csv`に書かれている聖遺物を上から使ってそれぞれの武器DPSを計算します　<br>

1. `Do you want to use cr/cd switch mode? [y|n]:`　<br>
冠自動変更モードを使用するかの選択肢です　<br>
使用する場合アクションリスト内で会心冠のステータスを記述する位置に`<crit>`と記述しておけば、　<br>
自動的に会心率冠、会心ダメージ冠の両方を使った場合のDPSが計算されます　<br>

## 🗂️　構成ファイル説明
### 📝　構成ディレクトリ・ファイル一覧

| 構成ディレクトリ・ファイル名 | 説明 |
| :--- | :--- |
| AutoWeaponCalc.sln | 本プロジェクトのソリューションファイル |
| Dockerfile | コンテナ環境を構築する際の設定ファイル |
| docker-compose.yml | コンテナ環境を実行する際の設定ファイル |
| README.md | この説明書きファイル |
| bin | 実行可能バイナリ出力先ディレクトリ |
| out | 実行後に生成される計算結果CSV出力先ディレクトリ |
| out/WeaponDps_◯◯_◯◯.csv or WeaponDps.csv | 実行後に生成される計算結果CSV<br>WeaponDps_◯◯_◯◯.csvの場合は聖遺物セットの名前が入る<br> WeaponDps.csvは武器のみの計算の場合に出力されるファイル名 |
| resource | 実行時に使用するリソース(ファイルなど)の配置先ディレクトリ |
| resource/execBinary | gcsimの実行可能バイナリの配置先ディレクトリ |
| resource/execBinary/gcsim.exe | gcsimの実行可能バイナリ |
| resource/input  | 実行時に使用する設定値ファイルの配置先ディレクトリ|
| resource/input/artifacts.csv | 実行時に計算したい聖遺物セットをまとめたCSVファイル <br><br><details><summary>詳細内容</summary>聖遺物モードを使用する場合、2行目から、「<4セットか>,<聖遺物名1>,<聖遺物名2>」の様式で記述してください<br><4セットか><br>`0`: 4セットではない<br>`1`: 4セットである<br><聖遺物名1> ここに書いたとおりにconfig.txtへ書き込まれます<br><聖遺物名2> 4セットの場合0と入力してください<br>上から聖遺物ごとにCSVを分けて出力します<br>出力はWeaponDPS_聖遺物名.csvに出ます<br></details> |
| resource/input/config.txt | 実行時に計算したいgcsimのアクションリストを計算可能にした設定値ファイル <br><br><details><summary>詳細内容</summary>gcsimのコンフィグファイルです　<br>計算したいキャラの武器名に`<w>`、精錬ランクに`<r>`、聖遺物名に`<a>`、ピース数に`<p>`を力しておくと自動で置き換えて連続計算します　<br>置き換えてほしくない場合は普通に書き込んでください　<br></details> |
| resource/input/example | 実行時に使用する設定値ファイルサンプルをまとめたディレクトリ |
| resource/weaponData | 実行時に使用する武器ごとの情報をまとめたCSVの配置先ディレクトリ |
| src | ソースコードの配置先ディレクトリ |
| src/AWC.cs | 本コンソールアプリケーションの本体コードファイル |
| src/common | 共通処理のコードの配置先ディレクトリ |
| src/config | 定数や設定値などのコードの配置先ディレクトリ |
| src/enum | 列挙型のコードの配置先ディレクトリ |
| src/interface | インターフェースのコードの配置先ディレクトリ |
| src/module | インターフェースを実装したコードやアプリケーション機能に関わるコードの配置先ディレクトリ |
| test | テスト関連の配置先ディレクトリ |
| test/dummyText | テストに使用するダミーデータの配置先ディレクトリ |
| test/methods | メソッドごとに記述したテストコードの配置先ディレクトリ |
---
### 📁 ディレクトリ・ファイル構造
```log
.
├── AutoWeaponCalc.sln
├── Dockerfile 
├── docker-compose.yml 
├── README.md
├── bin 
│   ├── AutoWeaponCalc 
│   ├── AutoWeaponCalc.deps.json
│   ├── AutoWeaponCalc.dll 
│   ├── AutoWeaponCalc.pdb
│   └── AutoWeaponCalc.runtimeconfig.json
├── out 
│   └──WeaponDps_◯◯_◯◯.csv or WeaponDps.csv 
├── resource 
│   ├── execBinary 
│   │   └── gcsim.exe or gcsim binary 
│   ├── input
│   │   ├── artifacts.csv
│   │   ├── config.txt 
│   │   └── example
│   └── weaponData 
│       ├── bow.csv
│       ├── catalyst.csv
│       ├── claymore.csv
│       ├── polearm.csv
│       └── sword.csv
│   └── chatracterData
│       └── character.csv
├── src 
│   ├── AWC.cs 
│   ├── AutoWeaponCalc.csproj
│   ├── common 
│   ├── config 
│   ├── enum 
│   ├── interface 
│   └── module 
└── test 
    ├── dummyText
    └── methods
```

## 📃 起動例
```log
mode selection(auto / manual) [a|m] :
a
Type the name of the character to calculate:
xingqiu
Type the weapon type of the character to calculate [sword|claymore|bow|catalyst|polearm] :
sword
Substat optimization in progress...
Substat optimization completed
mistsplitterreforged:14559.18
Substat optimization in progress...
Substat optimization completed
            ︙
            ︙
Substat optimization in progress...
Substat optimization completed
dullblade:7163.31
Calculation completed for artifact sr
Substat optimization in progress...
Substat optimization completed
mistsplitterreforged:14522.15
Substat optimization in progress...
Substat optimization completed
aquilafavonia:12590.48
            ︙
            ︙
Substat optimization in progress...
Substat optimization completed
dullblade:7871.13
Calculation completed for artifact gd
```
