using CalcsheetGenerator;

namespace Test.Methods
{
        [Collection("singletonのためテスト直列化")]
    public class ReplaceTextTest
    {
        [Theory(DisplayName="武器と聖遺物の置き換え")]
        [InlineData(
            "xingqiu add weapon=\"<w>\" refine=<r>",
            "xingqiu add weapon=\"sacrificialsword\" refine=5",
            6,
            "xingqiu add weapon=\"sacrificialsword\" refine=5 lvl=90/90;")]
        [InlineData(
            "xingqiu add set=\"<a>\" count=<p>;",
            "xingqiu add set=\"eosf\" count=4;",
            7,
            "xingqiu add set=\"eosf\" count=4;")]
        public void Nomal(string OldText, string NewText, int Index, string ReplaceText)
        {
            string[] Content = new[] {
                "options swap_delay=12 iteration=1000 ;",
                "sucrose char lvl=90/90 cons=6 talent=9,9,9;",
                "sucrose add weapon=\"hakushin\" refine=5 lvl=90/90;",
                "sucrose add set=\"viridescentvenerer\" count=5;",
                "sucrose add stats hp=4780 atk=311 em=559.5 ; #main",
                "xingqiu char lvl=90/90 cons=6 talent=9,9,9;",
                "xingqiu add weapon=\"<w>\" refine=<r> lvl=90/90;",
                "xingqiu add set=\"<a>\" count=<p>;",
                "xingqiu add stats hp=4780 atk=311 atk%=0.466 hydro%=0.466 cr=0.311 ; #main",
                "fischl char lvl=90/90 cons=6 talent=9,9,9;",
                "fischl add weapon=\"thestringless\" refine=3 lvl=90/90;",
                "fischl add set=\"totm\" count=4;",
                "fischl add stats hp=4780 atk=311 atk%=0.466 electro%=0.466 cr=0.311 ; #main",
                "beidou char lvl=90/90 cons=6 talent=9,9,9;",
                "beidou add weapon=\"serpentspine\" refine=1 lvl=90/90 +params=[stacks=5];",
                "beidou add set=\"eosf\" count=4;",
                "beidou add stats hp=4780 atk=311 atk%=0.466 electro%=0.466 cd=0.622 ; #main",
                "sucrose add stats def%=0.124 def=39.36 hp=507.88 hp%=0.0992 atk=33.08 atk%=0.5952 er=0.3306 em=118.92 cr=0.1324 cd=0.1324;",
                "xingqiu add stats def%=0.124 def=39.36 hp=507.88 hp%=0.0992 atk=33.08 atk%=0.0992 er=0.551 em=39.64 cr=0.2317 cd=0.5958;",
                "beidou add stats def%=0.124 def=39.36 hp=507.88 hp%=0.0992 atk=33.08 atk%=0.0992 er=0.3306 em=39.64 cr=0.3641 cd=0.5958;",
                "active beidou;",
                "target lvl=100 pos=0,2.4 radius=2 resist=.1 hp=999999999999;",
                "energy every interval=480,720 amount=1;",
                "for let i = 0; i < 4; i = i + 1 {",
                "    beidou skill, burst, attack;",
                "    fischl skill, attack;",
                "    xingqiu burst, attack, skill, dash, attack:2;",
                "    sucrose attack:2, skill, dash,",
                "            attack:3, dash, ",
                "            attack:2, burst;",
                "    while .fischl.oz-duration > 180 {",
                "        if .sucrose.skill.charge > 1 && .sucrose.normal > 3 {",
                "            sucrose skill, dash;",
                "        }",
                "        else {",
                "            sucrose attack;",
                "        }",
                "    }",
                "    beidou skill[counter=1], attack;",
                "    fischl attack:2, burst;",
                "    sucrose swap;",
                "    while .fischl.oz-duration > 120 {",
                "        if .sucrose.skill.charge > 1 && .sucrose.normal > 3 {",
                "            sucrose skill, dash;",
                "        }",
                "        else {",
                "            sucrose attack;",
                "        }",
                "    }",
                "print\"done\");",
                "}"
            };
            
            string actual = Primary.ReplaceText(string.Join(Environment.NewLine, Content), OldText, NewText);
            
            Content[Index] = ReplaceText;
            string expect = string.Join(Environment.NewLine, Content);
            Assert.Equal(expect, actual);
        }
    }
}