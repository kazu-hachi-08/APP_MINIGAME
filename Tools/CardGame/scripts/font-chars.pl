#!/usr/bin/perl
# 画面に出る文字を集める(scripts/subset-font.sh から使う)。
#   perl scripts/font-chars.pl <出力ファイル>
# - C# の文字列リテラル("..." と $"..."。コメントは除く)
# - カード / デッキの JSON(名前・効果・フレーバー)
# - ASCII と、よく使う記号・全角英数(動的に組み立てる文字列用)
# かなは全種、漢字は使われているものだけ(1 回の取得で送れるのは約 800 字まで)。
use strict;
use warnings;
use utf8;
use open qw(:std :encoding(UTF-8));

my %c;
# --names: カード名に出る文字だけ(カード名用の書体。かな・英数も足す)
if (grep { $_ eq "--names" } @ARGV) {
    for my $f (glob("Core/CardGame.Core/Data/Resources/cards/*.json")) {
        open my $fh, "<:encoding(UTF-8)", $f or die "$f: $!";
        local $/; my $t = <$fh>;
        while ($t =~ /"name":\s*"([^"]*)"/g) { $c{$_} = 1 for split //, $1 }
    }
    $c{chr($_)} = 1 for (0x20 .. 0x7E, 0x30FC, 0x30FB, 0x3041 .. 0x3093, 0x30A1 .. 0x30F6);
    my @all = sort keys %c;
    open my $out, ">", $ARGV[0] or die;
    print $out join("", @all);
    printf STDERR "カード名の文字数 %d\n", scalar @all;
    exit 0;
}
my @cs = (glob("Unity/Assets/Scripts/*/*.cs"), glob("Core/CardGame.Core/*/*.cs"));
for my $f (@cs) {
    open my $fh, "<:encoding(UTF-8)", $f or die "$f: $!";
    while (my $line = <$fh>) {
        $line =~ s{//.*$}{};                       # 行コメント(/// も)を除く
        while ($line =~ /"((?:[^"\\]|\\.)*)"/g) { $c{$_} = 1 for split //, $1 }
    }
}
for my $f (glob("Core/CardGame.Core/Data/Resources/cards/*.json"), glob("Core/CardGame.Core/Data/Resources/decks/*.json")) {
    open my $fh, "<:encoding(UTF-8)", $f or die "$f: $!";
    local $/; my $t = <$fh>;
    $c{$_} = 1 for split //, $t;
}
$c{chr($_)} = 1 for (0x20 .. 0x7E, 0x00D7, 0x2015, 0x2025, 0x2026, 0x2160, 0x2161, 0x2190 .. 0x2193,
                     0x25A0, 0x25A1, 0x25B2, 0x25B6, 0x25BC, 0x25C0, 0x25CF, 0x2605, 0x2606,
                     0x3000 .. 0x3003, 0x3005, 0x3008 .. 0x3011, 0x301C, 0x30FB, 0x30FC, 0xFF01 .. 0xFF0F, 0xFF1A .. 0xFF20,
                     0x3041 .. 0x3093, 0x30A1 .. 0x30F6);   # かなは全種(新しい文言でも欠けないように)
my @all = sort grep { $_ ne "\n" && $_ ne "\r" && $_ ne "\t" } keys %c;
open my $out, ">", $ARGV[0] or die;
print $out join("", @all);
printf STDERR "文字数 %d\n", scalar @all;
