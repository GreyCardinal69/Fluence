using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Fluence.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkDotNet.Running.BenchmarkRunner.Run<Benchmarker>();
            Console.ReadLine();
        }
    }

    [MemoryDiagnoser]
    [EvaluateOverhead]
    public class Benchmarker
    {
        [Benchmark]
        public void FluenceConwaysWayOfLife_500GEN_20x10Grid_Bench()
        {
            FluenceInterpreter interpreter = new FluenceInterpreter();
            interpreter.Compile(FluenceConwaysWayOfLife_500GEN_20x10Grid, false);
            interpreter.RunUntilDone();
        }

        [Benchmark]
        public void FluenceBillionIterations()
        {
            FluenceInterpreter interpreter = new FluenceInterpreter();
            interpreter.Compile(BillionIterationsFluence, false);
            interpreter.RunUntilDone();
        }

        [Benchmark]
        public void FluenceMillionShieve()
        {
            FluenceInterpreter interpreter = new FluenceInterpreter();
            interpreter.Compile(MillionShieveFluence, false);
            interpreter.RunUntilDone();
        }

        [Benchmark]
        public void FluenceCollatzConjecture1To100000()
        {
            FluenceInterpreter interpreter = new FluenceInterpreter();
            interpreter.Compile(FluenceCollatzConjecture100000, true);
            interpreter.RunUntilDone();
        }

        [Benchmark]
        public void FluenceLevenshteinFluenceMIN()
        {
            FluenceInterpreter interpreter = new FluenceInterpreter();
            interpreter.Compile(flulev2, true);
            interpreter.RunUntilDone();
        }

        [Benchmark]
        public void FluenceFibonacchiRecursiveN__30()
        {
            FluenceInterpreter interpreter = new FluenceInterpreter();
            interpreter.Compile(fluenceFibRecursive, true);
            interpreter.RunUntilDone();
        }

        [Benchmark]
        public void FluenceLexer()
        {
            FluenceLexer lexer = new FluenceLexer(source);
            while (!lexer.HasReachedEnd) lexer.ConsumeToken();
        }

        [Benchmark]
        public void FluenceParserAndLexer()
        {
            FluenceLexer lexer = new FluenceLexer(source);
            FluenceParser parser = new FluenceParser(lexer, new VirtualMachineConfiguration() { OptimizeByteCode = true }, null, null, null);
            parser.Parse(true);
        }

        #region TEST_CODE   
        private static readonly string flulev2 = @"
            use FluenceIO;
            use FluenceMath;
 

            func levenshtein(s1, s2) => {
                m, n <~| s1.length(), s2.length();

 
                dp <~| [0..n];
 

                for i in 1..m {
 
                    prev_row_prev_col <~| i - 1;
         
                    dp[0] = i;

                    for j in 1..n {
 
                        temp <~| dp[j];
            
                        cost <~| 0;
                        if s1[i-1] != s2[j-1] -> cost = 1;
 
                        dp[j] = min(dp[j] + 1, dp[j-1] + 1, prev_row_prev_col + cost);

                        prev_row_prev_col = temp;
                    }
                }

                return dp[n];
            } 
             func min(a, b, c) => (a < b ? (a < c ? a : c) : (b < c ? b : c));
            func Main() => {
                  S1,s2 <~| ""..."", ""..."";
                dist <~| levenshtein(s1, s2);
    
            }
        ";

        private static readonly string source = @"
            space MyMath_1 {
                struct Vector3_1 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_1 {
                b,
                c,
            }

            struct Globuloid_1 {
                Name = ""globuloid"";
            }

            space MyProgram_1 {

                use MyMath_1;
                use FluenceMath;

                struct Number_1 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_1Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_1() => {
                    number_1 = Number_1(10, Number_1Type.Float);
                    print(number_1);
                    Helper_1();

                    position3D_1 = Vector3_1(1,2,3);
                    print(position3D_1);
                }

                func Helper_1() => { print(""helper""); }
            }


            space MyMath_2 {
                struct Vector3_2 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_2 {
                b,
                c,
            }

            struct Globuloid_2 {
                Name = ""globuloid"";
            }

            space MyProgram_2 {

                use MyMath_2;
                use FluenceMath;

                struct Number_2 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_2Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_2() => {
                    number_2 = Number_2(10, Number_2Type.Float);
                    print(number_2);
                    Helper_2();

                    position3D_2 = Vector3_2(1,2,3);
                    print(position3D_2);
                }

                func Helper_2() => { print(""helper""); }
            }


            space MyMath_3 {
                struct Vector3_3 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_3 {
                b,
                c,
            }

            struct Globuloid_3 {
                Name = ""globuloid"";
            }

            space MyProgram_3 {

                use MyMath_3;
                use FluenceMath;

                struct Number_3 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_3Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_3() => {
                    number_3 = Number_3(10, Number_3Type.Float);
                    print(number_3);
                    Helper_3();

                    position3D_3 = Vector3_3(1,2,3);
                    print(position3D_3);
                }

                func Helper_3() => { print(""helper""); }
            }


            space MyMath_4 {
                struct Vector3_4 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_4 {
                b,
                c,
            }

            struct Globuloid_4 {
                Name = ""globuloid"";
            }

            space MyProgram_4 {

                use MyMath_4;
                use FluenceMath;

                struct Number_4 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_4Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_4() => {
                    number_4 = Number_4(10, Number_4Type.Float);
                    print(number_4);
                    Helper_4();

                    position3D_4 = Vector3_4(1,2,3);
                    print(position3D_4);
                }

                func Helper_4() => { print(""helper""); }
            }


            space MyMath_5 {
                struct Vector3_5 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_5 {
                b,
                c,
            }

            struct Globuloid_5 {
                Name = ""globuloid"";
            }

            space MyProgram_5 {

                use MyMath_5;
                use FluenceMath;

                struct Number_5 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_5Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_5() => {
                    number_5 = Number_5(10, Number_5Type.Float);
                    print(number_5);
                    Helper_5();

                    position3D_5 = Vector3_5(1,2,3);
                    print(position3D_5);
                }

                func Helper_5() => { print(""helper""); }
            }


            space MyMath_6 {
                struct Vector3_6 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_6 {
                b,
                c,
            }

            struct Globuloid_6 {
                Name = ""globuloid"";
            }

            space MyProgram_6 {

                use MyMath_6;
                use FluenceMath;

                struct Number_6 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_6Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_6() => {
                    number_6 = Number_6(10, Number_6Type.Float);
                    print(number_6);
                    Helper_6();

                    position3D_6 = Vector3_6(1,2,3);
                    print(position3D_6);
                }

                func Helper_6() => { print(""helper""); }
            }


            space MyMath_7 {
                struct Vector3_7 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_7 {
                b,
                c,
            }

            struct Globuloid_7 {
                Name = ""globuloid"";
            }

            space MyProgram_7 {

                use MyMath_7;
                use FluenceMath;

                struct Number_7 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_7Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_7() => {
                    number_7 = Number_7(10, Number_7Type.Float);
                    print(number_7);
                    Helper_7();

                    position3D_7 = Vector3_7(1,2,3);
                    print(position3D_7);
                }

                func Helper_7() => { print(""helper""); }
            }


            space MyMath_8 {
                struct Vector3_8 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_8 {
                b,
                c,
            }

            struct Globuloid_8 {
                Name = ""globuloid"";
            }

            space MyProgram_8 {

                use MyMath_8;
                use FluenceMath;

                struct Number_8 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_8Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_8() => {
                    number_8 = Number_8(10, Number_8Type.Float);
                    print(number_8);
                    Helper_8();

                    position3D_8 = Vector3_8(1,2,3);
                    print(position3D_8);
                }

                func Helper_8() => { print(""helper""); }
            }


            space MyMath_9 {
                struct Vector3_9 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_9 {
                b,
                c,
            }

            struct Globuloid_9 {
                Name = ""globuloid"";
            }

            space MyProgram_9 {

                use MyMath_9;
                use FluenceMath;

                struct Number_9 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_9Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_9() => {
                    number_9 = Number_9(10, Number_9Type.Float);
                    print(number_9);
                    Helper_9();

                    position3D_9 = Vector3_9(1,2,3);
                    print(position3D_9);
                }

                func Helper_9() => { print(""helper""); }
            }


            space MyMath_10 {
                struct Vector3_10 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_10 {
                b,
                c,
            }

            struct Globuloid_10 {
                Name = ""globuloid"";
            }

            space MyProgram_10 {

                use MyMath_10;
                use FluenceMath;

                struct Number_10 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_10Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_10() => {
                    number_10 = Number_10(10, Number_10Type.Float);
                    print(number_10);
                    Helper_10();

                    position3D_10 = Vector3_10(1,2,3);
                    print(position3D_10);
                }

                func Helper_10() => { print(""helper""); }
            }


            space MyMath_11 {
                struct Vector3_11 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_11 {
                b,
                c,
            }

            struct Globuloid_11 {
                Name = ""globuloid"";
            }

            space MyProgram_11 {

                use MyMath_11;
                use FluenceMath;

                struct Number_11 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_11Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_11() => {
                    number_11 = Number_11(10, Number_11Type.Float);
                    print(number_11);
                    Helper_11();

                    position3D_11 = Vector3_11(1,2,3);
                    print(position3D_11);
                }

                func Helper_11() => { print(""helper""); }
            }


            space MyMath_12 {
                struct Vector3_12 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_12 {
                b,
                c,
            }

            struct Globuloid_12 {
                Name = ""globuloid"";
            }

            space MyProgram_12 {

                use MyMath_12;
                use FluenceMath;

                struct Number_12 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_12Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_12() => {
                    number_12 = Number_12(10, Number_12Type.Float);
                    print(number_12);
                    Helper_12();

                    position3D_12 = Vector3_12(1,2,3);
                    print(position3D_12);
                }

                func Helper_12() => { print(""helper""); }
            }


            space MyMath_13 {
                struct Vector3_13 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_13 {
                b,
                c,
            }

            struct Globuloid_13 {
                Name = ""globuloid"";
            }

            space MyProgram_13 {

                use MyMath_13;
                use FluenceMath;

                struct Number_13 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_13Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_13() => {
                    number_13 = Number_13(10, Number_13Type.Float);
                    print(number_13);
                    Helper_13();

                    position3D_13 = Vector3_13(1,2,3);
                    print(position3D_13);
                }

                func Helper_13() => { print(""helper""); }
            }


            space MyMath_14 {
                struct Vector3_14 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_14 {
                b,
                c,
            }

            struct Globuloid_14 {
                Name = ""globuloid"";
            }

            space MyProgram_14 {

                use MyMath_14;
                use FluenceMath;

                struct Number_14 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_14Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_14() => {
                    number_14 = Number_14(10, Number_14Type.Float);
                    print(number_14);
                    Helper_14();

                    position3D_14 = Vector3_14(1,2,3);
                    print(position3D_14);
                }

                func Helper_14() => { print(""helper""); }
            }


            space MyMath_15 {
                struct Vector3_15 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_15 {
                b,
                c,
            }

            struct Globuloid_15 {
                Name = ""globuloid"";
            }

            space MyProgram_15 {

                use MyMath_15;
                use FluenceMath;

                struct Number_15 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_15Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_15() => {
                    number_15 = Number_15(10, Number_15Type.Float);
                    print(number_15);
                    Helper_15();

                    position3D_15 = Vector3_15(1,2,3);
                    print(position3D_15);
                }

                func Helper_15() => { print(""helper""); }
            }


            space MyMath_16 {
                struct Vector3_16 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_16 {
                b,
                c,
            }

            struct Globuloid_16 {
                Name = ""globuloid"";
            }

            space MyProgram_16 {

                use MyMath_16;
                use FluenceMath;

                struct Number_16 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_16Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_16() => {
                    number_16 = Number_16(10, Number_16Type.Float);
                    print(number_16);
                    Helper_16();

                    position3D_16 = Vector3_16(1,2,3);
                    print(position3D_16);
                }

                func Helper_16() => { print(""helper""); }
            }


            space MyMath_17 {
                struct Vector3_17 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_17 {
                b,
                c,
            }

            struct Globuloid_17 {
                Name = ""globuloid"";
            }

            space MyProgram_17 {

                use MyMath_17;
                use FluenceMath;

                struct Number_17 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_17Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_17() => {
                    number_17 = Number_17(10, Number_17Type.Float);
                    print(number_17);
                    Helper_17();

                    position3D_17 = Vector3_17(1,2,3);
                    print(position3D_17);
                }

                func Helper_17() => { print(""helper""); }
            }


            space MyMath_18 {
                struct Vector3_18 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_18 {
                b,
                c,
            }

            struct Globuloid_18 {
                Name = ""globuloid"";
            }

            space MyProgram_18 {

                use MyMath_18;
                use FluenceMath;

                struct Number_18 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_18Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_18() => {
                    number_18 = Number_18(10, Number_18Type.Float);
                    print(number_18);
                    Helper_18();

                    position3D_18 = Vector3_18(1,2,3);
                    print(position3D_18);
                }

                func Helper_18() => { print(""helper""); }
            }


            space MyMath_19 {
                struct Vector3_19 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_19 {
                b,
                c,
            }

            struct Globuloid_19 {
                Name = ""globuloid"";
            }

            space MyProgram_19 {

                use MyMath_19;
                use FluenceMath;

                struct Number_19 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_19Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_19() => {
                    number_19 = Number_19(10, Number_19Type.Float);
                    print(number_19);
                    Helper_19();

                    position3D_19 = Vector3_19(1,2,3);
                    print(position3D_19);
                }

                func Helper_19() => { print(""helper""); }
            }


            space MyMath_20 {
                struct Vector3_20 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_20 {
                b,
                c,
            }

            struct Globuloid_20 {
                Name = ""globuloid"";
            }

            space MyProgram_20 {

                use MyMath_20;
                use FluenceMath;

                struct Number_20 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_20Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_20() => {
                    number_20 = Number_20(10, Number_20Type.Float);
                    print(number_20);
                    Helper_20();

                    position3D_20 = Vector3_20(1,2,3);
                    print(position3D_20);
                }

                func Helper_20() => { print(""helper""); }
            }


            space MyMath_21 {
                struct Vector3_21 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_21 {
                b,
                c,
            }

            struct Globuloid_21 {
                Name = ""globuloid"";
            }

            space MyProgram_21 {

                use MyMath_21;
                use FluenceMath;

                struct Number_21 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_21Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_21() => {
                    number_21 = Number_21(10, Number_21Type.Float);
                    print(number_21);
                    Helper_21();

                    position3D_21 = Vector3_21(1,2,3);
                    print(position3D_21);
                }

                func Helper_21() => { print(""helper""); }
            }


            space MyMath_22 {
                struct Vector3_22 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_22 {
                b,
                c,
            }

            struct Globuloid_22 {
                Name = ""globuloid"";
            }

            space MyProgram_22 {

                use MyMath_22;
                use FluenceMath;

                struct Number_22 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_22Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_22() => {
                    number_22 = Number_22(10, Number_22Type.Float);
                    print(number_22);
                    Helper_22();

                    position3D_22 = Vector3_22(1,2,3);
                    print(position3D_22);
                }

                func Helper_22() => { print(""helper""); }
            }


            space MyMath_23 {
                struct Vector3_23 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_23 {
                b,
                c,
            }

            struct Globuloid_23 {
                Name = ""globuloid"";
            }

            space MyProgram_23 {

                use MyMath_23;
                use FluenceMath;

                struct Number_23 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_23Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_23() => {
                    number_23 = Number_23(10, Number_23Type.Float);
                    print(number_23);
                    Helper_23();

                    position3D_23 = Vector3_23(1,2,3);
                    print(position3D_23);
                }

                func Helper_23() => { print(""helper""); }
            }


            space MyMath_24 {
                struct Vector3_24 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_24 {
                b,
                c,
            }

            struct Globuloid_24 {
                Name = ""globuloid"";
            }

            space MyProgram_24 {

                use MyMath_24;
                use FluenceMath;

                struct Number_24 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_24Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_24() => {
                    number_24 = Number_24(10, Number_24Type.Float);
                    print(number_24);
                    Helper_24();

                    position3D_24 = Vector3_24(1,2,3);
                    print(position3D_24);
                }

                func Helper_24() => { print(""helper""); }
            }


            space MyMath_25 {
                struct Vector3_25 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_25 {
                b,
                c,
            }

            struct Globuloid_25 {
                Name = ""globuloid"";
            }

            space MyProgram_25 {

                use MyMath_25;
                use FluenceMath;

                struct Number_25 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_25Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_25() => {
                    number_25 = Number_25(10, Number_25Type.Float);
                    print(number_25);
                    Helper_25();

                    position3D_25 = Vector3_25(1,2,3);
                    print(position3D_25);
                }

                func Helper_25() => { print(""helper""); }
            }


            space MyMath_26 {
                struct Vector3_26 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_26 {
                b,
                c,
            }

            struct Globuloid_26 {
                Name = ""globuloid"";
            }

            space MyProgram_26 {

                use MyMath_26;
                use FluenceMath;

                struct Number_26 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_26Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_26() => {
                    number_26 = Number_26(10, Number_26Type.Float);
                    print(number_26);
                    Helper_26();

                    position3D_26 = Vector3_26(1,2,3);
                    print(position3D_26);
                }

                func Helper_26() => { print(""helper""); }
            }


            space MyMath_27 {
                struct Vector3_27 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_27 {
                b,
                c,
            }

            struct Globuloid_27 {
                Name = ""globuloid"";
            }

            space MyProgram_27 {

                use MyMath_27;
                use FluenceMath;

                struct Number_27 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_27Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_27() => {
                    number_27 = Number_27(10, Number_27Type.Float);
                    print(number_27);
                    Helper_27();

                    position3D_27 = Vector3_27(1,2,3);
                    print(position3D_27);
                }

                func Helper_27() => { print(""helper""); }
            }


            space MyMath_28 {
                struct Vector3_28 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_28 {
                b,
                c,
            }

            struct Globuloid_28 {
                Name = ""globuloid"";
            }

            space MyProgram_28 {

                use MyMath_28;
                use FluenceMath;

                struct Number_28 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_28Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_28() => {
                    number_28 = Number_28(10, Number_28Type.Float);
                    print(number_28);
                    Helper_28();

                    position3D_28 = Vector3_28(1,2,3);
                    print(position3D_28);
                }

                func Helper_28() => { print(""helper""); }
            }


            space MyMath_29 {
                struct Vector3_29 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_29 {
                b,
                c,
            }

            struct Globuloid_29 {
                Name = ""globuloid"";
            }

            space MyProgram_29 {

                use MyMath_29;
                use FluenceMath;

                struct Number_29 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_29Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_29() => {
                    number_29 = Number_29(10, Number_29Type.Float);
                    print(number_29);
                    Helper_29();

                    position3D_29 = Vector3_29(1,2,3);
                    print(position3D_29);
                }

                func Helper_29() => { print(""helper""); }
            }


            space MyMath_30 {
                struct Vector3_30 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_30 {
                b,
                c,
            }

            struct Globuloid_30 {
                Name = ""globuloid"";
            }

            space MyProgram_30 {

                use MyMath_30;
                use FluenceMath;

                struct Number_30 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_30Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_30() => {
                    number_30 = Number_30(10, Number_30Type.Float);
                    print(number_30);
                    Helper_30();

                    position3D_30 = Vector3_30(1,2,3);
                    print(position3D_30);
                }

                func Helper_30() => { print(""helper""); }
            }


            space MyMath_31 {
                struct Vector3_31 {
                    x = 0;
                    y = 0;
                    z = 0;
                    func init(x,y,z) => {
                        self.x, self.y, self.z <~| x,y,z;
                    }
                }
            }

            enum A_31 {
                b,
                c,
            }

            struct Globuloid_31 {
                Name = ""globuloid"";
            }

            space MyProgram_31 {

                use MyMath_31;
                use FluenceMath;

                struct Number_31 {
                    num = 10;
                    num2 = 10 + 10;
                    numType = nil;

                    func Length() => (self.x**2 + self.y**2) ** 0.5;

                    func init(num, numType) => {
                        self.num, self.numType <~| num, numType;
                    }
                }

                enum Number_31Type {
                    Int,
                    Float,
                    Integer,
                }

                func Main_31() => {
                    number_31 = Number_31(10, Number_31Type.Float);
                    print(number_31);
                    Helper_31();

                    position3D_31 = Vector3_31(1,2,3);
                    print(position3D_31);
                }

                func Helper_31() => { print(""helper""); }
            }
                    ";

        private static readonly string fluenceFibRecursive = @"
            use FluenceIO;

            func fib(n) => {
                if n < 0 -> return 0;
                if n == 1 -> return 1;
                return fib(n-1) + fib(n-2);
            }

            func Main() => fib(30);
        ";

        private static readonly string FluenceCollatzConjecture100000 = @"
            func Collatz() => {
                max_len, num_with_max_len, limit <2| 0 <| 100000;

                for n in 1..limit {
                    len, term <~| 1, n;
                    while term != 1 {
                        if term % 2 == 0 -> term /= 2;
                        else -> term = term * 3 + 1;
                        len += 1;
                    }
                    if len > max_len -> max_len, num_with_max_len <~| len, n;
                } 
            }

            func Main() => {
                Collatz();
            }
        ";

        private static readonly string BillionIterationsFluence = @"func Main() => {
                timer = Stopwatch();
                timer.start();

                1_000_000_000 times  {
                    a = 5;
                    b = 5; 
                    c = a + b;
                }

                timer.stop();
              #  printl(f""Elapsed seconds: {timer.elapsed_ms() / 1000}"");
        }";

        private static readonly string MillionShieveFluence = @"use FluenceMath;
            use FluenceIO;

            func primes(limit) => {
                maxSquareRoot = (sqrt(limit));
                eliminated = [false] * (limit + 1);

                for i = 2; i <= maxSquareRoot; i += 1; {
                    if !eliminated[i] {
                        for j = i*i; j <= limit; j += i; {
                            eliminated[j] = true;
                        }
                    }
                }

                output = [];
                for i = 2; i <= limit; i += 1; {
                    if !eliminated[i] -> output.push(i);
                }
    
                return output;
            }

            func Main() => {
                n = 1_000_000;
    
                t0 = Time.now();
                x = primes(n);
                t1 = Time.now();
    
                duration = (t1 - t0) / 1000;

              #  printl(f""Found {x.length()} primes.""); # Should be 78498 for 1M
              #  printl(f""sieve({n}): {duration} sec"");
        }";

        private static readonly string FluenceConwaysWayOfLife_500GEN_20x10Grid = @"
            use FluenceIO;

            WIDTH = 20;
            HEIGHT = 10;
            TOTAL_CELLS = WIDTH * HEIGHT;

            func get_idx(x, y) => {
                wrapped_x, wrapped_y <~| (x + WIDTH) % WIDTH, (y + HEIGHT) % HEIGHT; 
                return wrapped_y * WIDTH + wrapped_x;
            }

            func count_neighbors(grid, x, y) => {
                count = 0;

                for dy in -1..1 -> for dx in (-1)..1 {
                        if dx, dy <==| 0 -> continue;
                        idx = get_idx(x + dx, y + dy);
                        if grid[idx] == 1 -> count++;
                    }
    
                return count;
            }

            func draw(grid, gen) => {
                buffer = f""\n--- Generation {gen} ---\n"";
    
                for y in 0..HEIGHT-1 {
                    line = """";
                    for x in 0..WIDTH-1 {
                        cell, char <~| grid[y * WIDTH + x], cell == 1 ?: ""O "", "". "";
                        line += char;
                    }
                    buffer = f""{buffer}{line}\n"";
                }

               # printl(buffer);
            }

            func Main() => {
                grid = [];
                next_grid = [];
    
                TOTAL_CELLS times { 
                    grid.push(0); 
                    next_grid.push(0); 
                };

                # 80 random cells across the grid.
                80 times {
                    grid[Random.between_exclusive(-1, TOTAL_CELLS)] = 1;
                } 

                generation = 0;
    
                500 times {
                    draw(grid, generation);
        
                    for y in 0..HEIGHT-1 -> for x in 0..WIDTH-1 {

                            current_idx, is_alive, neighbors, new_state <~| y * WIDTH + x, grid[current_idx], count_neighbors(grid, x, y), match neighbors {
                                3 -> 1;
                                2 -> is_alive;
                                rest -> 0;
                            };

                            next_grid[current_idx] = new_state;
                        }

                    grid >< next_grid;
        
                    generation++;
                }
    
              #  printl(""Simulation Complete."");
            }
        ";
        #endregion
    }
}