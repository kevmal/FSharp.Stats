﻿module SpecialFunctionsTests
open Expecto
    
open FSharp.Stats.SpecialFunctions
open FSharp.Stats

[<Tests>]
let gammaFunctionsTests =

    testList "SpecialFunctions.Gamma" [
        testCase "test_gamma" <| fun () ->
            let gam = Gamma.gamma 5.
            Expect.floatClose Accuracy.high gam  24. "Should be equal (double precision)"

        testCase "test_gammaln" <| fun () ->
            let gamln = Gamma.gammaLn 3.
            let lngam = Gamma.gamma 3. |> log
            Expect.floatClose Accuracy.high gamln lngam "Should be equal (double precision)"
 
        testCase "test_gammainc" <| fun () ->
            let gam = Gamma.lowerIncomplete 0.5 0.5
            Expect.floatClose Accuracy.low gam 0.682689 "Should be equal (low precision)"

        testCase "test_gammaincnan" <| fun () ->
            let gam = Gamma.lowerIncomplete -1. 1.
            Expect.isTrue (nan.Equals(gam)) "IsNan"

        testCase "test_gammainczero" <| fun () ->
            let gam = Gamma.lowerIncomplete -1. 0.
            Expect.floatClose Accuracy.high gam 0.0 "IsZero"

        testCase "test_gammaincinf" <| fun () ->
            let gam = Gamma.lowerIncomplete 0.5 Ops.inf
            Expect.equal gam 1.0 "Should be equal"

        testCase "test_gammaincupper" <| fun () ->
            let gamu = Gamma.upperIncomplete 0.5 0.5
            let gam  = 1. - Gamma.lowerIncomplete 0.5 0.5
            Expect.floatClose Accuracy.medium gamu gam "Should be equal (medium precision)"

        testCase "test_gammaincuppernan" <| fun () ->
            let gam = Gamma.upperIncomplete -1. 1.
            Expect.isTrue (nan.Equals(gam)) "IsNan"

        testCase "test_gammaincupperinf" <| fun () ->
            let gam = Gamma.upperIncomplete 0.5 Ops.inf
            Expect.equal gam 0.0 "Should be equal"

    ]    




[<Tests>]
let betaFunctionsTests =

    testList "SpecialFunctions.Beta" [
        testCase "betaLn equality1" <| fun () ->
            let result = Beta.betaLn 1. 1. 
            Expect.floatClose Accuracy.veryHigh result 0.0 "Should be equal (double precision)" //rtol=1e-14, atol=0
            //Expect.equal result 0.0 "Should be equal"
            //
        //testCase "equality2" <| fun () ->
        //    let result = Beta.betaLn -100.3 1e-200
        //    Expect.equal result (Gamma.gammaLn 1e-200) "Should be equal"

        testCase "betaLn equality3" <| fun () ->
            let result = Beta.betaLn 0.0342 170.
            Expect.floatClose Accuracy.veryHigh result 3.1811881124242447 "Should be equal (double precision)" //rtol=1e-14, atol=0
         
    ]

[<Tests>]
let factorialTests =
    //expected values taken from filling function values in wolfram alpha https://www.wolframalpha.com/
    testList "SpecialFunctions.Factorial" [
        testCase "Double overflow" (fun _ -> 
            Expect.equal (Factorial.factorial 171) infinity "Expected factorial of a number larger than 170 to result in infinity (171! is larger than max double)"
        )
        testCase "0! equals 1" (fun _ -> 
            Expect.equal (Factorial.factorial 0) 1. "Expected factorial of 0 to be 1."
        )
        testCase "69!" (fun _ -> 
            Expect.floatClose Accuracy.high (Factorial.factorial 69) 1.7112245e+98 "Expected factorial of 69 to be 1.7112245e+98"
        )
        testCase "factorial not defined for negative numbers" (fun _ -> 
            Expect.throws (fun _ -> Factorial.factorial -69421337 |> ignore) "Expected factorial to fail for negative values"
        )
    ]

[<Tests>]
let FactorialLnTests =
    //expected values taken from filling function values in wolfram alpha https://www.wolframalpha.com/
    testList "SpecialFunctions.lnFactorial" [
        testCase "Large value" (fun _ -> 
            Expect.floatClose Accuracy.high (Factorial.factorialLn 6942) 54467.727976695301612523565124699078303834231913072759124392135342 "factorialLn of large number failed"
        )
        testCase "ln(0!) equals 0" (fun _ -> 
            Expect.equal (Factorial.factorialLn 0) 0. "Expected factorialLn of 0 to be 1."
        )
        testCase "ln(69!)" (fun _ -> 
            Expect.floatClose Accuracy.high 226.19054832372759333227016852232261788323276357495863628461257077 (Factorial.factorialLn 69) "Expected factorialLn of 69 to be 226.19054832372759333227016852232261788323276357495863628461257077"
        )
        testCase "factorialLn not defined for negative numbers" (fun _ -> 
            Expect.throws (fun _ -> Factorial.factorialLn -69421337 |> ignore) "Expected factorialLn to fail for negative values"
        )
    ]

[<Tests>]
let logisticTests =
    //expected values taken from filling function values in wolfram alpha https://www.wolframalpha.com/
    testList "SpecialFunctions.Logistic" [
        testCase "standard x=69" (fun _ -> 
            Expect.floatClose Accuracy.low (Logistic.standard 2.) 0.8807970779778824440597291413023967952063842986289682757984052500 ""
        )
        testCase "standard nan" (fun _ -> 
            Expect.isTrue (nan.Equals(Logistic.standard nan)) "Expected nan"
        )
        testCase "standard inf" (fun _ -> 
            Expect.floatClose Accuracy.high (Logistic.standard infinity) 1. "Expected 1"
        )
        testCase "standard -inf" (fun _ -> 
            Expect.floatClose Accuracy.high (Logistic.standard (-infinity)) 0. "Expected 0"
        )
        testCase "generic x0=4 L=2 k=4 x=5 " (fun _ -> 
            Expect.floatClose Accuracy.high (Logistic.generic 4. 2. 4. 5. ) 1.9640275800758168839464137241009231502550299762409347760482632174 ""
        )
        //nan
        testCase "generic x=nan L=2 k=4 x0=4" (fun _ -> 
            Expect.isTrue (nan.Equals(Logistic.generic nan 2. 4. 5.)) "Expected nan"
        )
        testCase "generic x=4 L=nan k=4 x0=4" (fun _ -> 
            Expect.isTrue (nan.Equals(Logistic.generic 4. nan 4. 5.)) "Expected nan"
        )
        testCase "generic x=4 L=2 k=nan x0=4" (fun _ -> 
            Expect.isTrue (nan.Equals(Logistic.generic 4. 2. nan 5.)) "Expected nan"
        )
        testCase "generic x=4 L=2 k=4 x0=nan" (fun _ -> 
            Expect.isTrue (nan.Equals(Logistic.generic 4. 2. 4. nan)) "Expected nan"
        )
        //infinity
        testCase "generic x=infinity L=2 k=4 x0=4" (fun _ -> 
            Expect.floatClose Accuracy.high  (Logistic.generic infinity 2. 4. 5.) 0. "Expected 0"
        )
        testCase "generic x=4 L=infinity k=4 x0=4" (fun _ -> 
            Expect.isTrue (infinity.Equals(Logistic.generic 4. infinity 4. 5.)) "Expected infinity"
        )
        testCase "generic x=4 L=2 k=infinity x0=4" (fun _ -> 
            Expect.floatClose Accuracy.high (Logistic.generic 4. 2. infinity 5.) 2. "Expected 2"
        )
        testCase "generic x=4 L=2 k=4 x0=infinity" (fun _ -> 
            Expect.floatClose Accuracy.high (Logistic.generic 4. 2. 4. infinity) 2. "Expected 2"
        )
        //-infinity
        testCase "generic x=-infinity L=2 k=4 x0=4" (fun _ -> 
            Expect.floatClose Accuracy.low 2. (Logistic.generic (-infinity) 2. 4. 5.) "Expected 2"
        )
        testCase "generic x=4 L=-infinity k=4 x0=4" (fun _ -> 
            Expect.isTrue ((-infinity).Equals(Logistic.generic 4. (-infinity) 4. 5.)) "Expected -infinity"
        )
        testCase "generic x=4 L=2 k=-infinity x0=4" (fun _ -> 
            Expect.floatClose Accuracy.high 0. (Logistic.generic 4. 2. (-infinity) 5.)  "Expected 0"
        )
        testCase "generic x=4 L=2 k=4 x0=-infinity" (fun _ -> 
            Expect.floatClose Accuracy.high 0. (Logistic.generic 4. 2. 4. (-infinity))  "Expected 0"
        )
    ]


[<Tests>]
let erfTests =
    //expected values taken from filling function values in wolfram alpha https://www.wolframalpha.com/
    testList "SpecialFunctions.ErrorFunction(erf)" [
        // erf
        testCase "erf(0) = 0" (fun _ -> 
            Expect.floatClose Accuracy.medium (Errorfunction.Erf 0.) 0. "expected erf(0) to be 0"
        )
        testCase "erf(-3)" (fun _ -> 
            Expect.floatClose Accuracy.medium (Errorfunction.Erf -3.) -0.999977909503001414558627223870417679620152292912600750342761045 "erf returned insufficient approximation of the result"
        )
        testCase "erf(3)" (fun _ -> 
            Expect.floatClose Accuracy.medium (Errorfunction.Erf 3.) 0.9999779095030014145586272238704176796201522929126007503427610451 "erf returned insufficient approximation of the result"
        )
        testCase "erf(nan)" (fun _ -> 
            Expect.isTrue (nan.Equals(Errorfunction.Erf nan)) "Expected nan"
        )
        testCase "erf(infinity)" (fun _ -> 
            Expect.equal  (Errorfunction.Erf infinity) 1. "expected erf(infinity) to be 1"
        )
        testCase "erf(-infinity)" (fun _ -> 
            Expect.equal  (Errorfunction.Erf -infinity) -1. "expected erf(-infinity) to be -1"
        )
        //erfc
        testCase "erfc(0) = 1" (fun _ -> 
            Expect.floatClose Accuracy.low (Errorfunction.Erfc 0.) 1. "expected erfc(0) to be 1"
        )
        testCase "erfc(-3)" (fun _ -> 
            Expect.floatClose Accuracy.low (Errorfunction.Erfc -3.) 1.9999779095030014145586272238704176796201522929126007503427610451 "erfc returned insufficient approximation of the result"
        )
        testCase "erfc(3)" (fun _ -> 
            Expect.floatClose Accuracy.low (Errorfunction.Erfc 3.) 0.0000220904969985854413727761295823203798477070873992496572389548 "erfc returned insufficient approximation of the result"
        )
        testCase "erfc(nan)" (fun _ -> 
            Expect.isTrue (nan.Equals(Errorfunction.Erfc nan)) "Expected nan"
        )
        testCase "erfc(infinity)" (fun _ -> 
            Expect.equal  (Errorfunction.Erfc infinity) 0. "expected erfc(infinity) to be 0"
        )
        testCase "erfc(-infinity)" (fun _ -> 
            Expect.equal  (Errorfunction.Erfc -infinity) 2. "expected erfc(-infinity) to be 2"
        )
        //erfcx
        testCase "erfcx(0) = 1" (fun _ -> 
            Expect.floatClose Accuracy.low (Errorfunction.erfcx 0.) 1. "expected erfcx(0) to be 1"
        )
        testCase "erfcx(-3)" (fun _ -> 
            Expect.floatClose Accuracy.low (Errorfunction.erfcx -3.) 16205.988853999586625469574084050206309035724190299120070784655345 "erfcx returned insufficient approximation of the result"
        )
        testCase "erfcx(3)" (fun _ -> 
            Expect.floatClose Accuracy.low (Errorfunction.erfcx 3.) 0.1790011511813899504192948153136209872279853641068542156627588395 "erfcx returned insufficient approximation of the result"
        )
        testCase "erfcx(nan)" (fun _ -> 
            Expect.isTrue (nan.Equals(Errorfunction.erfcx nan)) "Expected nan"
        )
        testCase "erfcx(infinity)" (fun _ -> 
            Expect.isTrue (nan.Equals(Errorfunction.erfcx infinity)) "expected erfcx(infinity) to be nan"
        )
        testCase "erfcx(-infinity)" (fun _ -> 
            Expect.isTrue (infinity.Equals(Errorfunction.erfcx -infinity)) "expected erfcx(-infinity) to be infinity"
        )
    ]

[<Tests>]
let binomialCoefficientTests =
    //expected values taken from filling function values in wolfram alpha https://www.wolframalpha.com/
    testList "SpecialFunctions.Binomial" [
        testCase "(0 | 0) = 1" (fun _ -> 
            Expect.floatClose Accuracy.high (Binomial.coeffcient 0 0) 1. "Expected (0 | 0) to be 1"
        )
        testCase "(-1 | 0) should throw (negative value)" (fun _ -> 
            Expect.throws (fun _ -> (Binomial.coeffcient -1 0) |> ignore) "Expected (-1 | 0) to throw"
        )
        testCase "(0 | -1) should throw (negative value)" (fun _ -> 
            Expect.throws (fun _ -> (Binomial.coeffcient 0 -1) |> ignore) "Expected (0 | -1) to throw"
        )
        testCase "(1 | 2) should throw (n<k)" (fun _ -> 
            Expect.throws (fun _ -> (Binomial.coeffcient 1 2) |> ignore) "Expected (1 | 2) to throw"
        )
        testCase "(69 | 42)" (fun _ -> 
            Expect.floatClose Accuracy.high (Binomial.coeffcient 69 42) 11185257572725865552. "Binomail coefficient returned wrong result"
        )
        // coefficientLn
        testCase "ln(0 | 0) = 1" (fun _ -> 
            Expect.floatClose Accuracy.high (Binomial.coeffcientLn 0 0) 0. "Expected ln(0 | 0) to be 0"
        )
        testCase "ln(-1 | 0) should throw (negative value)" (fun _ -> 
            Expect.throws (fun _ -> (Binomial.coeffcientLn -1 0) |> ignore) "Expected ln(-1 | 0) to throw"
        )
        testCase "ln(0 | -1) should throw (negative value)" (fun _ -> 
            Expect.throws (fun _ -> (Binomial.coeffcientLn 0 -1) |> ignore) "Expected ln(0 | -1) to throw"
        )
        testCase "ln(1 | 2) should throw (n<k)" (fun _ -> 
            Expect.throws (fun _ -> (Binomial.coeffcientLn 1 2) |> ignore) "Expected ln(1 | 2) to throw"
        )
        testCase "ln(69 | 42)" (fun _ -> 
            Expect.floatClose Accuracy.high (Binomial.coeffcientLn 69 42) 43.861128296976190734480722409484720407496953168236423941162466992 "Binomail coefficientln returned wrong result"
        )
        testCase "ln(69000 | 4200)" (fun _ -> 
            Expect.floatClose Accuracy.high (Binomial.coeffcientLn 69000 4200) 15820.331735478233070945558688627562355591359160763289683149612673 "Binomail coefficientln returned wrong result"
        )
    ]