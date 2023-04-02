# AutoWeaponCalc (AWC)
## 起動例
```
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

動画用にキャラの武器比較をつくるとき、いちいちgcsimのsubstat optimizerモードを何度も使って計算するのが面倒だったので、
一連の手順を自動化するために作ったコンソールアプリケーションです　<br>

config.txtの編成、ローテーションを使って、指定したキャラの武器ごとの個人DPSを計算し、csvに出力します　<br>


## つかいかた

1. run.batで起動

2. `mode selection(auto / manual) [ a | m ]`　<br>
モード設定です　<br>
autoに設定すると5と6にそれぞれ0とyを入れて飛ばします　<br>

3. `Type the name of the character to calculate:`　<br>
計算したいキャラクターの名前を入力してください　<br>
キャラ名はconfigファイルに記載のある通りに入力してください　<br>

4. `Type the weapon type of the character to calculate [sword|claymore|bow|catalyst|polearm] :`　<br>
キャラクターの武器種を設定してください(ここ手動ですごめんなさい)　<br>
武器csvファイルの名前を指定しているだけなので別にcsvを用意して同じディレクトリにぶち込めば>カスタム武器リストをつくれます　<br>

5. `Type the refinement rank of the weapon to calculate [0=auto][1-5] :`　<br>
武器の精錬ランクを設定してください　<br>
0にすると⭐5はR1、⭐4はR5で計算されます　<br>
武器csvのrarityで管理しています(1=⭐5, 0=⭐4)　<br>

6. `Do you want to use artifact mode? [y|n]:`　<br>
聖遺物モードを使用するかの選択肢です　<br>
使用する場合artifacts.csvに書かれている聖遺物を上から使ってそれぞれの武器DPSを計算します　<br>


## 構成ファイル説明

- config.txt　<br>
gcsimのコンフィグファイルです　<br>
計算したいキャラの武器名に`<w>`、精錬ランクに`<r>`、聖遺物名に`<a>`、ピース数に`<p>`を入力しておくと自動で置き換えて連続計算します　<br>
置き換えてほしくない場合は普通に書き込んでください　<br>

- artifacts.csv　<br>
聖遺物モードを使用する場合、2行目から、「<4セットか>,<聖遺物名1>,<聖遺物名2>」の様式で記述してください　<br>

> <4セットか>   
> `0`: 4セットではない  
> `1`: 4セットである  
> <聖遺物名1> ここに書いたとおりにconfig.txtへ書き込まれます  
> <聖遺物名2> 4セットの場合0と入力してください  
> 上から聖遺物ごとにCSVを分けて出力します  
> 出力はTable_聖遺物名.csvに出ます
