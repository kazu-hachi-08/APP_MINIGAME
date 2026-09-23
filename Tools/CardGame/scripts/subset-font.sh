#!/usr/bin/env bash
# 日本語フォント(Noto Serif JP Bold)を、画面に出る文字だけに絞って作り直す(WebGL の容量削減。2026-09-23)。
#
#   bash scripts/subset-font.sh
#
# - 画面に出る文字 = scripts/font-chars.pl が集める(C# の文字列リテラル・カード/デッキの JSON・ASCII・かな全種・記号)
# - Google Fonts の「text= で指定した文字だけのフォントを返す」機能で取得する(Noto Serif JP は OFL)
# - 1 回で送れるのは約 800 字まで。超えると全文字版(7MB)が返ってくるので止める
# - フォントは 1 つにする(2 つに分けてフォールバックさせる方式は WebGL で効かず、字が抜けた)
# カードや画面の文言に新しい漢字を足したら、これを実行し直す(足りない字は表示されない)。
set -euo pipefail
cd "$(dirname "$0")/.."
OUT=Unity/Assets/Resources/Fonts/NotoSerifJP-Bold.ttf
TMP=$(mktemp -d)
perl scripts/font-chars.pl "$TMP/chars.txt"
enc=$(perl -e 'local $/; open my $f, "<", $ARGV[0]; my $t = <$f>; $t =~ s/([^A-Za-z0-9\-_.~])/sprintf("%%%02X", ord($1))/ge; print $t' "$TMP/chars.txt")
url=$(curl -sf -m 60 -A "curl/8.0" "https://fonts.googleapis.com/css2?family=Noto+Serif+JP:wght@700&text=$enc" | grep -o 'https://fonts.gstatic.com[^)]*')
curl -sf -m 60 -o "$TMP/font.ttf" "$url"
size=$(wc -c < "$TMP/font.ttf")
if [ "$size" -gt 2000000 ]; then echo "文字数が多すぎて全文字版が返ってきた($size バイト)。font-chars.pl の範囲を見直す" >&2; exit 1; fi
cp "$TMP/font.ttf" "$OUT"
echo "$OUT: $size バイト"

# カード名用の書体(解星 徳明 ExtraBold。オーナー決定 2026-09-23)。カード名に出る文字 + かな + 英数だけ
perl scripts/font-chars.pl "$TMP/names.txt" --names
enc=$(perl -e 'local $/; open my $f, "<", $ARGV[0]; my $t = <$f>; $t =~ s/([^A-Za-z0-9-_.~])/sprintf("%%%02X", ord($1))/ge; print $t' "$TMP/names.txt")
url=$(curl -sf -m 60 -A "curl/8.0" "https://fonts.googleapis.com/css2?family=Kaisei+Tokumin:wght@800&text=$enc" | grep -o 'https://fonts.gstatic.com[^)]*')
curl -sf -m 60 -o "$TMP/name.ttf" "$url"
size=$(wc -c < "$TMP/name.ttf")
if [ "$size" -gt 2000000 ]; then echo "カード名の文字数が多すぎる($size バイト)" >&2; exit 1; fi
cp "$TMP/name.ttf" Unity/Assets/Resources/Fonts/KaiseiTokumin-ExtraBold.ttf
echo "KaiseiTokumin-ExtraBold.ttf: $size バイト"
rm -rf "$TMP"
