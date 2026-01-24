<#
.SYNOPSIS
    AIエージェント向けプリフライトスクリプト
    
.DESCRIPTION
    このスクリプトは、AIエージェントがプロジェクトのルールと作業履歴を
    確実に把握するために、毎回の応答生成前に実行されるべきです。
    
    出力内容:
    - .github/copilot-instructions.md（必読ルール）
    - docs/ 配下の全ドキュメント
    - .github/work-logs/ 配下の全作業ログ
    
.NOTES
    ファイル名: agent_preflight.ps1
    作成日: 2026-01-18
    
    【重要】OpenAI系Agentへの指示:
    このスクリプトの出力を全て読み、ルールに従ってください。
    特に .github/copilot-instructions.md のルールは絶対遵守です。
#>

$ErrorActionPreference = "Stop"
$OutputEncoding = [System.Text.Encoding]::UTF8

# プロジェクトルートを特定
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir

Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host "【AIエージェント プリフライトチェック】" -ForegroundColor Cyan
Write-Host "実行日時: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan
Write-Host "プロジェクトルート: $ProjectRoot" -ForegroundColor Cyan
Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host ""

# ========================================
# セクション1: 必読ルール (copilot-instructions.md)
# ========================================
Write-Host "=" * 80 -ForegroundColor Red
Write-Host "【セクション1: 必読ルール - copilot-instructions.md】" -ForegroundColor Red
Write-Host "★★★ このセクションのルールは絶対遵守 ★★★" -ForegroundColor Yellow
Write-Host "=" * 80 -ForegroundColor Red
Write-Host ""

$CopilotInstructions = Join-Path $ProjectRoot ".github\copilot-instructions.md"
if (Test-Path $CopilotInstructions) {
    Write-Host "--- BEGIN: copilot-instructions.md ---" -ForegroundColor Green
    Get-Content $CopilotInstructions -Encoding UTF8 -Raw
    Write-Host ""
    Write-Host "--- END: copilot-instructions.md ---" -ForegroundColor Green
} else {
    Write-Host "[ERROR] copilot-instructions.md が見つかりません: $CopilotInstructions" -ForegroundColor Red
}
Write-Host ""

# ========================================
# セクション2: プロジェクトドキュメント (docs/)
# ========================================
Write-Host "=" * 80 -ForegroundColor Blue
Write-Host "【セクション2: プロジェクトドキュメント (docs/)】" -ForegroundColor Blue
Write-Host "=" * 80 -ForegroundColor Blue
Write-Host ""

$DocsDir = Join-Path $ProjectRoot "docs"
if (Test-Path $DocsDir) {
    $DocsFiles = Get-ChildItem -Path $DocsDir -Filter "*.md" -File | Sort-Object Name
    Write-Host "ドキュメント数: $($DocsFiles.Count)" -ForegroundColor Cyan
    Write-Host ""
    
    foreach ($Doc in $DocsFiles) {
        Write-Host "-" * 60 -ForegroundColor DarkGray
        Write-Host "【$($Doc.Name)】" -ForegroundColor Yellow
        Write-Host "-" * 60 -ForegroundColor DarkGray
        Get-Content $Doc.FullName -Encoding UTF8 -Raw
        Write-Host ""
    }
} else {
    Write-Host "[WARNING] docs/ フォルダが見つかりません: $DocsDir" -ForegroundColor Yellow
}
Write-Host ""

# ========================================
# セクション3: 作業ログ (.github/work-logs/)
# ========================================
Write-Host "=" * 80 -ForegroundColor Magenta
Write-Host "【セクション3: 作業ログ (.github/work-logs/)】" -ForegroundColor Magenta
Write-Host "★ 過去の作業履歴を確認し、重複や矛盾を避けてください ★" -ForegroundColor Yellow
Write-Host "=" * 80 -ForegroundColor Magenta
Write-Host ""

$WorkLogsDir = Join-Path $ProjectRoot ".github\work-logs"
if (Test-Path $WorkLogsDir) {
    $WorkLogs = Get-ChildItem -Path $WorkLogsDir -Filter "*.md" -File | Sort-Object Name -Descending
    Write-Host "作業ログ数: $($WorkLogs.Count)" -ForegroundColor Cyan
    Write-Host ""
    
    # 全ての作業ログを出力
    Write-Host "【全作業ログ（新しい順）】" -ForegroundColor Green
    foreach ($Log in $WorkLogs) {
        Write-Host "-" * 60 -ForegroundColor DarkGray
        Write-Host "【$($Log.Name)】" -ForegroundColor Yellow
        Write-Host "-" * 60 -ForegroundColor DarkGray
        Get-Content $Log.FullName -Encoding UTF8 -Raw
        Write-Host ""
    }
} else {
    Write-Host "[WARNING] .github/work-logs/ フォルダが見つかりません: $WorkLogsDir" -ForegroundColor Yellow
}
Write-Host ""

# ========================================
# セクション4: 最終確認
# ========================================
Write-Host "=" * 80 -ForegroundColor Red
Write-Host "【重要: 作業前の最終確認】" -ForegroundColor Red
Write-Host "=" * 80 -ForegroundColor Red
$FinalCheckMsg = @"

1. ユーザー対応は「日本語」、コードコメントは「英語」。
2. .github/work-logs/ を確認し、作業後は必ずログを作成する。
3. コマンド実行前は必ず「許可」を得る。
4. エラー/Lint警告を無視せず、修正してからコミットする。
5. 「完了」ではなく「確認依頼」を行う。

> 上記 copilot-instructions.md のルールを遵守して作業を開始してください。

"@

Write-Host $FinalCheckMsg -ForegroundColor Yellow

Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host "プリフライトチェック完了" -ForegroundColor Cyan
Write-Host "=" * 80 -ForegroundColor Cyan
