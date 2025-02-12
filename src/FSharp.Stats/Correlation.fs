namespace FSharp.Stats
/// Contains correlation functions for different data types 
module Correlation =

    /// This module contains normalization and helper functions for the biweighted midcorrelation
    module private bicorHelpers = 

        let sumBy2 f (a1 : float []) (a2 : float []) = 
            let mutable sum = 0.
            for i = 0 to a1.Length - 1 do
                sum <- sum + (f a1.[i] a2.[i])
            sum

        let sumBy4 f (a1 : float []) (a2 : float []) (a3 : float []) (a4 : float [])= 
            let mutable sum = 0.
            for i = 0 to a1.Length - 1 do
                sum <- sum + (f a1.[i] a2.[i] a3.[i] a4.[i])
            sum    

        let identity (x: float) = 
            if x > 0. then 1. else 0.

        let deviation med mad value = 
            (value - med) / (9. * mad)

        let weight dev = ((1. - (dev ** 2.)) ** 2.) * (identity (1. - (abs dev)))

        let getNormalizationFactor (values: float []) (weights:float[]) (med:float) = 
            sumBy2 (fun value weight -> 
                ((value - med) * weight) ** 2.
                )
                values
                weights
            |> sqrt

        let normalize (value:float) (weight:float) normalizationFactor med =   
            normalizationFactor
            |> (/) ((value - med) * weight)

    /// Contains correlation functions optimized for sequences
    [<RequireQualifiedAccess>]
    module Seq = 
        /// <summary>Calculates the pearson correlation of two samples. Homoscedasticity must be assumed.</summary>
        /// <remarks></remarks>
        /// <param name="seq1"></param>
        /// <param name="seq2"></param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// </code>
        /// </example>
        let inline pearson (seq1:seq<'T>) (seq2:seq<'T>) : float =
            if Seq.length seq1 <> Seq.length seq2 then failwithf "input arguments are not the same length"
            let seq1' = seq1 |> Seq.map float
            let seq2' = seq2 |> Seq.map float
            use e = seq1'.GetEnumerator()
            use e2 = seq2'.GetEnumerator()
            let zero = float (LanguagePrimitives.GenericZero<'T>)
            let one = float (LanguagePrimitives.GenericOne<'T>)
            let rec loop n sumX sumY sumXY sumXX sumYY = 
                match (e.MoveNext() && e2.MoveNext()) with
                    | true  -> 
                        loop (n + one) (sumX + e.Current) (sumY + e2.Current) (sumXY + (e.Current * e2.Current)) (sumXX + (e.Current * e.Current)) (sumYY + (e2.Current * e2.Current))
                    | false -> 
                        if n > zero then  
                            // Covariance
                            let cov = float ((sumXY * n) - (sumX * sumY))
                            // Standard Deviation
                            let stndDev1 = sqrt (float ((n * sumXX) - (sumX * sumX)))
                            let stndDev2 = sqrt (float ((n * sumYY) - (sumY * sumY)))
                            // Correlation
                            let tmp = cov / (stndDev1 * stndDev2)
                            // TODO: solve in a prettier coding fashion
                            if tmp >= 1. then 1. 
                            elif tmp <= -1. then -1.
                            else tmp              
                        
                        else nan
            loop zero zero zero zero zero zero

        /// <summary>
        /// Calculates the pearson correlation of two samples given as a sequence of paired values. 
        /// Homoscedasticity must be assumed.
        /// </summary>
        /// <param name="seq">The input sequence.</param>
        /// <typeparam name="'T"></typeparam>
        /// <returns>The pearson correlation.</returns>
        /// <example> 
        /// <code> 
        /// // Consider a sequence of paired x and y values:
        /// // [(x1, y1); (x2, y2); (x3, y3); (x4, y4); ... ]
        /// let xy = [(312.7, 315.5); (104.2, 101.3); (104.0, 108.0); (34.7, 32.2)]
        /// 
        /// // To get the correlation between x and y:
        /// xy |> Seq.pearsonOfPairs // evaluates to 0.9997053729
        /// </code>
        /// </example>
        let inline pearsonOfPairs (seq:seq<'T * 'T>) = 
            seq
            |> Seq.toArray
            |> Array.unzip
            ||> pearson

        /// <summary>
        /// Calculates the pearson correlation of two samples.
        /// The two samples are built by applying the given function to each element of the sequence.
        /// The function should transform each sequence element into a tuple of paired observations from the two samples.
        /// The correlation will be calculated between the paired observations.
        /// </summary>
        /// <param name="f">A function applied to transform each element of the sequence into a tuple of paired observations.</param>
        /// <param name="seq">The input sequence.</param>
        /// <returns>The pearson correlation.</returns>
        /// <example>
        /// <code>
        /// // To get the correlation between A and B observations:
        /// let ab = [ {| A = 312.7; B = 315.5 |}
        ///            {| A = 104.2; B = 101.3 |}
        ///            {| A = 104.; B = 108. |}
        ///            {| A = 34.7; B = 32.2 |} ]
        /// 
        /// ab |> Seq.pearsonBy (fun x -> x.A, x.B) // evaluates to 0.9997053729
        /// </code>
        /// </example>
        let inline pearsonBy f (seq: 'T seq) =
            seq
            |> Seq.map f
            |> pearsonOfPairs

        /// <summary>weighted pearson correlation (http://sci.tech-archive.net/Archive/sci.stat.math/2006-02/msg00171.html)</summary>
        /// <remarks></remarks>
        /// <param name="seq1"></param>
        /// <param name="seq2"></param>
        /// <param name="weights"></param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// </code>
        /// </example>
        let inline pearsonWeighted (seq1:seq<'T>) (seq2:seq<'T>) (weights:seq<'T>) : float =
            // TODO: solve in a prettier coding fashion
            if Seq.length seq1 <> Seq.length seq2 || Seq.length seq2 <> Seq.length weights then failwithf "input arguments are not the same length"
            let zero = LanguagePrimitives.GenericZero< 'T > 
            let one = LanguagePrimitives.GenericOne<'T> 
            let weightedMean xVal wVal = 
                let a = Seq.fold2 (fun acc xi wi -> acc + (xi * wi)) zero xVal wVal |> float
                let b = Seq.sum wVal|> float
                a / b
            let weightedCoVariance xVal yVal wVal = 
                let weightedMeanXW = weightedMean xVal wVal
                let weightedMeanYW = weightedMean yVal wVal
                let a = 
                    Seq.map3 (fun xi yi wi -> 
                        (float wi) * ((float xi) - weightedMeanXW) * ((float yi) - weightedMeanYW)
                            ) xVal yVal wVal
                    |> Seq.sum
                let b = 
                    Seq.sum wVal 
                    |> float
                a / b
            let weightedCorrelation xVal yVal wVal =
                let a = weightedCoVariance xVal yVal wVal
                let b = 
                    (weightedCoVariance xVal xVal wVal) * (weightedCoVariance yVal yVal wVal)
                    |> sqrt
                a / b          
            weightedCorrelation seq1 seq2 weights

        /// <summary>
        /// Calculates the weighted pearson correlation of two samples. 
        /// If the two samples are <c>x</c> and <c>y</c> then the elements of the input sequence should be triples of <c>x * y * weight</c>.
        /// </summary>
        /// <param name="seq">The input sequence.</param>
        /// <returns>The weighted pearson correlation.</returns>
        /// <example>
        /// <code>
        /// // Consider a sequence of triples with x, y and w values:
        /// // [(x1, y1, w1); (x2, y2, w2); (x3, y3, w3); (x4, y4, w4); ... ]
        /// let xyw = [(1.1, 1.2, 0.2); (1.1, 0.9, 0.3); (1.2, 0.08, 0.5)] 
        /// 
        /// // To get the correlation between x and y weighted by w :
        /// xyw |> Seq.pearsonWeightedOfTriples // evaluates to -0.9764158959
        /// </code>
        /// </example>
        let inline pearsonWeightedOfTriples (seq: seq<'T * 'T * 'T>) =
            seq
            |> Seq.toArray
            |> Array.unzip3
            |||> pearsonWeighted

        /// <summary>
        /// Calculates the weighted pearson correlation of two samples after 
        /// applying the given function to each element of the sequence.
        /// </summary>
        /// <param name="f">A function applied to transform each element of the sequence into a tuple of triples representing the two samples and the weight. If the two samples are <c>x</c> and <c>y</c> then the elements of the sequence should be triples of <c>x * y * weight</c></param>
        /// <param name="seq">The input sequence.</param>
        /// <returns>The weighted pearson correlation.</returns>
        /// <example>
        /// <code>
        /// // To get the correlation between A and B observations weighted by W:
        /// let abw = [ {| A = 1.1; B = 1.2; W = 0.2 |}
        ///             {| A = 1.1; B = 0.9; W = 0.3 |}
        ///             {| A = 1.2; B = 0.08; W = 0.5 |} ] 
        /// 
        /// abw |> pearsonWeightedBy (fun x -> x.A, x.B, x.W)
        /// // evaluates to -0.9764158959
        /// </code>
        /// </example>
        let inline pearsonWeightedBy f (seq: 'T seq) =
            seq
            |> Seq.map f
            |> pearsonWeightedOfTriples

        ///<summary>
        /// Spearman Correlation (with ranks)
        /// 
        /// Items that are tied are each allocated the average of the ranks that
        /// they would have had had they been distinguishable.
        /// 
        /// Reference: Williams R.B.G. (1986) Spearman’s and Kendall’s Coefficients of Rank Correlation. 
        /// Intermediate Statistics for Geographers and Earth Scientists, p453, https://doi.org/10.1007/978-1-349-06813-5_6
        /// </summary>
        /// <returns>The spearman correlation.</returns>
        /// <example>
        /// <code>
        /// let x = [5.05;6.75;3.21;2.66]
        /// let y = [1.65;2.64;2.64;6.95]
        /// 
        /// // To get the correlation between x and y:
        /// Seq.spearman x y // evaluates to -0.632455532
        /// </code>
        /// </example>
        let inline spearman (seq1: seq<'T>) (seq2: seq<'T>) : float =
    
            let spearRank1 = seq1 |> Seq.map float |> Seq.toArray |> FSharp.Stats.Rank.RankAverage()
            let spearRank2 = seq2 |> Seq.map float |> Seq.toArray |> FSharp.Stats.Rank.RankAverage()

            pearson spearRank1 spearRank2

        /// <summary>
        /// Calculates the spearman correlation (with ranks) of two samples given as a sequence of paired values. 
        /// </summary>
        /// <param name="seq">The input sequence.</param>
        /// <typeparam name="'T"></typeparam>
        /// <returns>The spearman correlation.</returns>
        /// <example> 
        /// <code> 
        /// // Consider a sequence of paired x and y values:
        /// // [(x1, y1); (x2, y2); (x3, y3); (x4, y4); ... ]
        /// let xy = [(1.1, 1.2); (1.1, 0.9); (2.0, 3.85)]
        /// 
        /// // To get the correlation between x and y:
        /// xy |> Seq.spearmanOfPairs // evaluates to 0.5
        /// </code>
        /// </example>
        let inline spearmanOfPairs (seq:seq<'T * 'T>) = 
            seq
            |> Seq.toArray
            |> Array.unzip
            ||> spearman

        /// <summary>
        /// Calculates the spearman correlation of two samples.
        /// The two samples are built by applying the given function to each element of the sequence.
        /// The function should transform each sequence element into a tuple of paired observations from the two samples.
        /// The correlation will be calculated between the paired observations.
        /// </summary>
        /// <param name="f">A function applied to transform each element of the sequence into a tuple of paired observations.</param>
        /// <param name="seq">The input sequence.</param>
        /// <returns>The spearman correlation.</returns>
        /// <example>
        /// <code>
        /// // To get the correlation between A and B observations:
        /// let ab = [ {| A = 1.1; B = 1.2 |}
        ///            {| A = 1.1; B = 0.9 |}
        ///            {| A = 2.0; B = 3.85 |} ] 
        /// 
        /// ab |> Seq.spearmanBy (fun x -> x.A, x.B) // evaluates to 0.5
        /// </code>
        /// </example>
        let inline spearmanBy f (seq: 'T seq) =
            seq
            |> Seq.map f
            |> spearmanOfPairs

            
        module internal Kendall = 
            // x: 'a[] -> y: 'b[] -> pq: float -> n0: int -> n1: int -> n2: int -> 'c
            // - x: The first array of observations.
            // - y: The second array of observations.
            // - pq: Number of concordant minues the number of discordant pairs.
            // - n0: n(n-1)/2 or (n choose 2), where n is the number of observations.
            // - n1: sum_i(t_i(t_i-1)/2) where t_is is t_i he number of pairs of observations with the same x value.
            // - n2: sum_i(u_i(u_i-1)/2) where u_is is u_i he number of pairs of observations with the same y value.
            
            /// <summary>
            /// Tau A - Make no adjustments for ties
            /// </summary>
            /// <param name="x">The first array of observations.</param>
            /// <param name="y">The second array of observations.</param>
            /// <param name="pq">Number of concordant minues the number of discordant pairs.</param>
            /// <param name="n0">n(n-1)/2 or (n choose 2), where n is the number of observations.</param>
            /// <param name="n1">sum_i(t_i(t_i-1)/2) where t_is is t_i he number of pairs of observations with the same x value.</param>
            /// <param name="n2">sum_i(u_i(u_i-1)/2) where u_is is u_i he number of pairs of observations with the same y value.</param>
            /// <returns>The Kendall tau A statistic.</returns>
            let tauA _x _y pq n0 _n1 _n2  = pq / float n0
            /// <summary>
            /// Tau B - Adjust for ties. tau_b = pq / sqrt((n0 - n1)(n0 - n2))
            /// </summary>
            /// <param name="x">The first array of observations.</param>
            /// <param name="y">The second array of observations.</param>
            /// <param name="pq">Number of concordant minues the number of discordant pairs.</param>
            /// <param name="n0">n(n-1)/2 or (n choose 2), where n is the number of observations.</param>
            /// <param name="n1">sum_i(t_i(t_i-1)/2) where t_is is t_i he number of pairs of observations with the same x value.</param>
            /// <param name="n2">sum_i(u_i(u_i-1)/2) where u_is is u_i he number of pairs of observations with the same y value.</param>
            /// <returns>The Kendall tau B statistic.</returns>
            let tauB _x _y pq n0 n1 n2 =
                if n0 = n1 || n0 = n2 then nan else
                pq / sqrt (float (n0 - n1) * float (n0 - n2))
            /// <summary>
            /// Tau C - Adjust for ties in x and y. tau_c = 2pq / (n^2 * (m-1)/m) where m = min(distinct x, distinct y)
            /// </summary>
            /// <param name="x">The first array of observations.</param>
            /// <param name="y">The second array of observations.</param>
            /// <param name="pq">Number of concordant minues the number of discordant pairs.</param>
            /// <param name="n0">n(n-1)/2 or (n choose 2), where n is the number of observations.</param>
            /// <param name="n1">sum_i(t_i(t_i-1)/2) where t_is is t_i he number of pairs of observations with the same x value.</param>
            /// <param name="n2">sum_i(u_i(u_i-1)/2) where u_is is u_i he number of pairs of observations with the same y value.</param>
            /// <returns>The Kendall tau C statistic.</returns>
            let tauC (x : _[]) y pq _n0 _n1 _n2 = 
                let n = x.Length
                if n = 0 then nan else 
                let m = min (x |> Seq.distinct |> Seq.length) (y |> Seq.distinct |> Seq.length) |> double
                let d = double(n*n)*(m-1.)/m
                2.0*pq / d

        /// <summary>
        /// Computes the Kendall rank correlation coefficient between two sequences of observations. Tau function is provided as a parameter. 
        /// </summary>
        /// <remarks>
        /// The Kendall rank correlation coefficient is a statistic used to measure the ordinal association between two measured quantities.
        /// It is a measure of rank correlation: the similarity of the orderings of the data when ranked by each of the quantities.
        /// </remarks>
        /// <param name="tau">
        /// The Kendall tau function to use. x: 'a[] -> y: 'b[] -> pq: float -> n0: int -> n1: int -> n2: int -> 'c 
        /// <list>
        /// <item><term>x: </term><description>The first array of observations.</description></item>
        /// <item><term>y: </term><description>The second array of observations.</description></item>
        /// <item><term>pq: </term><description>Number of concordant minues the number of discordant pairs.</description></item>
        /// <item><term>n0: </term><description>n(n-1)/2 or (n choose 2), where n is the number of observations.</description></item>
        /// <item><term>n1: </term><description>sum_i(t_i(t_i-1)/2) where t_is is t_i he number of pairs of observations with the same x value.</description></item>
        /// <item><term>n2: </term><description>sum_i(u_i(u_i-1)/2) where u_is is u_i he number of pairs of observations with the same y value.</description></item>
        /// </list>
        /// The function would generally return the Kendall tau statistic, however, this setup to allow for returning other values, p-values, multiple statistics, etc.
        /// </param>
        /// <param name="x"> The first sequence of observations. </param>
        /// <param name="y"> The second sequence of observations. </param>
        let internal kendallTau tau (x: 'a[]) (y: 'b[]) =
            // Kendall's tau using the O(n log n) algorithm of Knight (1966).
            // - Initial sort by x, then by y.
            // - Count the number of swaps needed to sort by y.
            // - Count the number of concordant and discordant pairs.
            // - Count the number of ties in x, y, and both.
            // - Calculate the tau statistic.
            let n = min x.Length y.Length
            if n = 0 then
                tau x y nan 0 0 0
            else
                let a = [| 0 .. n - 1 |]
                let sortedIdx = a |> Array.sortBy (fun a -> x.[a], y.[a])
                let rec mergesort offset length =
                    match length with 
                    | 1 -> 0
                    | 2 -> 
                        if y.[sortedIdx.[offset]] <= y.[sortedIdx.[offset + 1]] then
                            0
                        else
                            Array.swapInPlace offset (offset + 1) sortedIdx
                            1
                    | _ ->
                        let leftLength = length / 2
                        let rightLength = length - leftLength
                        let middleIndex = offset + leftLength
                        let swaps = mergesort offset leftLength + mergesort middleIndex rightLength
                        if y.[sortedIdx.[middleIndex - 1]] < y.[sortedIdx.[middleIndex]] then
                            swaps
                        else
                            let rec merge i r l swaps =
                                if r < leftLength || l < rightLength then
                                    if l >= rightLength || (r < leftLength && y.[sortedIdx.[offset + r]] <= y.[sortedIdx.[middleIndex + l]]) then
                                        let d = i - r |> max 0
                                        let swaps = swaps + d
                                        a.[i] <- sortedIdx.[offset + r]
                                        merge (i + 1) (r + 1) l swaps
                                    else
                                        let d = (offset + i) - (middleIndex + l) |> max 0
                                        let swaps = swaps + d
                                        a.[i] <- sortedIdx.[middleIndex + l]
                                        merge (i + 1) r (l + 1) swaps
                                else
                                    swaps
                            let swaps = merge 0 0 0 swaps
                            Array.blit a 0 sortedIdx offset length
                            swaps
                let tallyTies noTie = 
                    let mutable k = 0
                    let mutable sum = 0
                    for i in 1 .. n - 1 do
                        if noTie k i then
                            sum <- sum + (i - k) * (i - k - 1) / 2
                            k <- i
                    sum + (n - k) * (n - k - 1) / 2
                let n3 = tallyTies (fun k i -> x.[sortedIdx.[k]] <> x.[sortedIdx.[i]] || y.[sortedIdx.[k]] <> y.[sortedIdx.[i]])
                let n1 = tallyTies (fun k i -> x.[sortedIdx.[k]] <> x.[sortedIdx.[i]])
                let swaps = mergesort 0 n
                let n2 = tallyTies (fun k i -> y.[sortedIdx.[k]] <> y.[sortedIdx.[i]])
                let n0 = n * (n - 1) / 2
                let pq = ((float (n0 - n1 - n2 + n3)) - 2.0 * float swaps)
                tau x y pq n0 n1 n2 
        
        /// <summary>Kendall Correlation Coefficient</summary>
        /// <remarks>Computes Kendall Tau-a rank correlation coefficient between two sequences of observations. No adjustment is made for ties.
        /// tau_a = (n_c - n_d) / n_0, where 
        /// <list>
        /// <item><term>n_c: </term><description>Number of concordant pairs.</description></item>
        /// <item><term>n_d: </term><description>Number of discordant pairs.</description></item>
        /// <item><term>n_0: </term><description>n*(n-1)/2 where n is the number of observations.</description></item>
        /// </list>
        /// </remarks>
        /// <param name="seq1">The first sequence of observations.</param>
        /// <param name="seq2">The second sequence of observations.</param>
        /// <returns>Kendall Tau-a rank correlation coefficient of setA and setB</returns>
        /// <example>
        /// <code>
        /// let x = [5.05;6.75;3.21;2.66]
        /// let y = [1.65;26.5;-0.64;6.95]
        ///
        /// Seq.kendallTauA x y // evaluates to 0.3333333333
        /// </code>
        /// </example>
        let kendallTauA seq1 seq2 = 
            let setA = Array.ofSeq seq1
            let setB = Array.ofSeq seq2
            if setB.Length <> setA.Length then invalidArg "seq2" "The input sequences must have the same length"
            kendallTau Kendall.tauA setA setB

        /// <summary>Kendall Correlation Coefficient</summary>
        /// <remarks>Computes Kendall Tau-b rank correlation coefficient between two sequences of observations. Tau-b is used to adjust for ties.
        /// tau_b = (n_c - n_d) / sqrt((n_0 - n_1) * (n_0 - n_2)), where
        /// <list>
        /// <item><term>n_c: </term><description>Number of concordant pairs.</description></item>
        /// <item><term>n_d: </term><description>Number of discordant pairs.</description></item>
        /// <item><term>n_0: </term><description>n*(n-1)/2 where n is the number of observations.</description></item>
        /// <item><term>n_1: </term><description>sum_i(t_i(t_i-1)/2) where t_i is the number of pairs of observations with the same x value.</description></item>
        /// <item><term>n_2: </term><description>sum_i(u_i(u_i-1)/2) where u_i is the number of pairs of observations with the same y value.</description></item>
        /// </list>
        /// </remarks>
        /// <param name="seq1">The first sequence of observations.</param>
        /// <param name="seq2">The second sequence of observations.</param>
        /// <returns>Kendall Tau-b rank correlation coefficient of seq1 and seq2</returns>
        /// <example>
        /// <code>
        /// let x = [5.05;6.75;3.21;2.66]
        /// let y = [1.65;26.5;-0.64;6.95]
        ///
        /// Seq.kendallTauB x y // evaluates to 0.3333333333
        /// </code>
        /// </example>
        let kendallTauB seq1 seq2 = 
            let setA = Array.ofSeq seq1
            let setB = Array.ofSeq seq2
            if setB.Length <> setA.Length then invalidArg "seq2" "The input sequences must have the same length"
            kendallTau Kendall.tauB setA setB
        
        /// <summary>Kendall Correlation Coefficient</summary>
        /// <remarks>Computes Kendall Tau-c rank correlation coefficient between two sequences of observations. Tau-c is used to adjust for ties which is preferred to Tau-b when x and y have a different number of possible values.
        /// tau_c = 2(n_c - n_d) / (n^2 * (m-1)/m), where
        /// <list>
        /// <item><term>n_c: </term><description>Number of concordant pairs.</description></item>
        /// <item><term>n_d: </term><description>Number of discordant pairs.</description></item>
        /// <item><term>n: </term><description>The number of observations.</description></item>
        /// <item><term>m: </term><description>The lesser of the distinct x count and distinct y count.</description></item>
        /// </list>
        /// </remarks>
        /// <param name="seq1">The first sequence of observations.</param>
        /// <param name="seq2">The second sequence of observations.</param>
        /// <returns>Kendall Tau-c rank correlation coefficient of seq1 and seq2</returns>
        /// <example>
        /// <code>
        /// let x = [1;1;1;2;2;2;3;3;3]
        /// let y = [2;2;4;4;6;6;8;8;10]
        ///
        /// Seq.kendallTauA x y // evaluates to 0.7222222222
        /// Seq.kendallTauB x y // evaluates to 0.8845379627
        /// Seq.kendallTauC x y // evaluates to 0.962962963
        /// </code>
        /// </example>
        let kendallTauC seq1 seq2 = 
            let setA = Array.ofSeq seq1
            let setB = Array.ofSeq seq2
            if setB.Length <> setA.Length then invalidArg "seq2" "The input sequences must have the same length"
            kendallTau Kendall.tauC setA setB

            
        /// <summary>Kendall Correlation Coefficient</summary>
        /// <remarks>This is an alias to <see cref="kendallTauB"/>. Computes Kendall Tau-b rank correlation coefficient between two sequences of observations. Tau-b is used to adjust for ties.
        /// tau_b = (n_c - n_d) / sqrt((n_0 - n_1) * (n_0 - n_2)), where
        /// <list>
        /// <item><term>n_c: </term><description>Number of concordant pairs.</description></item>
        /// <item><term>n_d: </term><description>Number of discordant pairs.</description></item>
        /// <item><term>n_0: </term><description>n*(n-1)/2 where n is the number of observations.</description></item>
        /// <item><term>n_1: </term><description>sum_i(t_i(t_i-1)/2) where t_i is the number of pairs of observations with the same x value.</description></item>
        /// <item><term>n_2: </term><description>sum_i(u_i(u_i-1)/2) where u_i is the number of pairs of observations with the same y value.</description></item>
        /// </list>
        /// </remarks>
        /// <param name="seq1">The first sequence of observations.</param>
        /// <param name="seq2">The second sequence of observations.</param>
        /// <returns>Kendall Tau-b rank correlation coefficient of seq1 and seq2</returns>
        /// <example>
        /// <code>
        /// let x = [5.05;6.75;3.21;2.66]
        /// let y = [1.65;26.5;-0.64;6.95]
        ///
        /// Seq.kendall x y // evaluates to 0.3333333333
        /// </code>
        /// </example>
        let kendall seq1 seq2 = kendallTauB seq1 seq2


        /// <summary>
        /// Calculates the kendall correlation coefficient of two samples given as a sequence of paired values. 
        /// </summary>
        /// <param name="seq">The input sequence.</param>
        /// <typeparam name="'T"></typeparam>
        /// <returns>The kendall correlation coefficient.</returns>
        /// <example> 
        /// <code> 
        /// // Consider a sequence of paired x and y values:
        /// // [(x1, y1); (x2, y2); (x3, y3); (x4, y4); ... ]
        /// let xy = [(-0.5, -0.3); (-0.4, -0.25); (0., -0.1); (0.7, -0.46); (0.65, 0.103); (0.9649, 0.409)] 
        /// 
        /// // To get the correlation between x and y:
        /// xy |> Seq.kendallOfPairs // evaluates to 0.4666666667
        /// </code>
        /// </example>
        let inline kendallOfPairs (seq:seq<'T * 'T>) = 
            seq
            |> Seq.toArray
            |> Array.unzip
            ||> kendall

        /// <summary>
        /// Calculates the kendall correlation of two samples.
        /// The two samples are built by applying the given function to each element of the sequence.
        /// The function should transform each sequence element into a tuple of paired observations from the two samples.
        /// The correlation will be calculated between the paired observations.
        /// </summary>
        /// <param name="f">A function applied to transform each element of the sequence into a tuple of paired observations.</param>
        /// <param name="seq">The input sequence.</param>
        /// <returns>The kendall correlation coefficient.</returns>
        /// <example>
        /// <code>
        /// // To get the correlation between A and B observations:
        /// let ab = [ {| A = -0.5; B = -0.3 |}
        ///            {| A = -0.4; B = -0.25 |}
        ///            {| A = 0.; B = -0.1 |}
        ///            {| A = 0.7; B = -0.46 |}
        ///            {| A = 0.65; B = 0.103 |}
        ///            {| A = 0.9649; B = 0.409 |} ]
        /// 
        /// ab |> Seq.kendallBy (fun x -> x.A, x.B) // evaluates to 0.4666666667
        /// </code>
        /// </example>
        let inline kendallBy f (seq: 'T seq) =
            seq
            |> Seq.map f
            |> kendallOfPairs

        /// <summary>Biweighted Midcorrelation. This is a median based correlation measure which is more robust against outliers.</summary>
        /// <remarks></remarks>
        /// <param name="seq1"></param>
        /// <param name="seq2"></param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// </code>
        /// </example>
        let bicor seq1 seq2 = 
            
            let xs,ys  = seq1 |> Array.ofSeq, seq2 |> Array.ofSeq

            let xMed = xs |> Array.median
            let xMad = xs |> Array.medianAbsoluteDev
            let xWeights = xs |> Array.map ((bicorHelpers.deviation xMed xMad) >> bicorHelpers.weight)
            let xNF = bicorHelpers.getNormalizationFactor xs xWeights xMed

            let yMed = ys |> Array.median
            let yMad = ys |> Array.medianAbsoluteDev
            let yWeights = ys |> Array.map ((bicorHelpers.deviation yMed yMad) >> bicorHelpers.weight)
            let yNF = bicorHelpers.getNormalizationFactor ys yWeights yMed

            bicorHelpers.sumBy4 (fun xVal xWeight yVal yWeight ->
                (bicorHelpers.normalize xVal xWeight xNF xMed) * (bicorHelpers.normalize yVal yWeight yNF yMed)
                )
                xs xWeights ys yWeights 

        /// <summary>
        /// Calculates the Biweighted Midcorrelation of two samples given as a sequence of paired values. 
        /// This is a median based correlation measure which is more robust against outliers.
        /// </summary>
        /// <param name="seq">The input sequence.</param>
        /// <typeparam name="'T"></typeparam>
        /// <returns>The Biweighted Midcorrelation.</returns>
        /// <example> 
        /// <code> 
        /// // Consider a sequence of paired x and y values:
        /// // [(x1, y1); (x2, y2); (x3, y3); (x4, y4); ... ]
        /// let xy = [(32.1, 1.2); (3.1, 0.4); (2.932, 3.85)]
        /// 
        /// // To get the correlation between x and y:
        /// xy |> Seq.bicorOfPairs // evaluates to -0.9303913046
        /// </code>
        /// </example>
        let inline bicorOfPairs seq = 
            seq
            |> Seq.toArray
            |> Array.unzip
            ||> bicor

        /// <summary>
        /// Calculates the bicor correlation of two samples.
        /// The two samples are built by applying the given function to each element of the sequence.
        /// The function should transform each sequence element into a tuple of paired observations from the two samples.
        /// The correlation will be calculated between the paired observations.
        /// </summary>
        /// <param name="f">A function applied to transform each element of the sequence into a tuple of paired observations.</param>
        /// <param name="seq">The input sequence.</param>
        /// <returns>The Biweighted Midcorrelation.</returns>
        /// <example>
        /// <code>
        /// // To get the correlation between A and B observations:
        /// let ab = [ {| A = 32.1; B = 1.2 |}
        ///            {| A = 3.1; B = 0.4 |}
        ///            {| A = 2.932; B = 3.85 |} ]
        /// 
        /// ab |> Seq.bicorBy (fun x -> x.A, x.B) // evaluates to -0.9303913046
        /// </code>
        /// </example>
        let inline bicorBy f (seq: 'T seq) =
            seq
            |> Seq.map f
            |> bicorOfPairs

    /// Contains correlation functions optimized for vectors
    [<AutoOpen>]
    module Vector =

        /// <summary>computes the sample correlation of two signal at a given lag.<br />was tested in comparison to: https://www.wessa.net/rwasp_autocorrelation.wasp</summary>
        /// <remarks></remarks>
        /// <param name="corrF"></param>
        /// <param name="lag"></param>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// </code>
        /// </example>
        let correlationOf (corrF: vector -> vector -> float) lag (v1:vector) (v2:vector) = 
            if v1.Length <> v2.Length then failwithf "Vectors need to have the same length."
            if lag >= v1.Length then failwithf "lag must be smaller than input length"
            let v1' = v1.[0..(v1.Length-1 - lag)]
            let v2' = v2.[lag..] 
            corrF v1' v2'

        /// <summary>computes the sample auto correlation (using pearson correlation) of a signal at a given lag.</summary>
        /// <remarks></remarks>
        /// <param name="lag"></param>
        /// <param name="v1"></param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// </code>
        /// </example>
        let autoCorrelation lag v1 = 
            correlationOf Seq.pearson lag v1 v1

        /// <summary>computes the sample auto corvariance of a signal at a given lag.</summary>
        /// <remarks></remarks>
        /// <param name="lag"></param>
        /// <param name="seq"></param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// </code>
        /// </example>
        let autoCovariance lag seq = 
            correlationOf Vector.cov lag seq seq

        /// <summary>computes the normalized (using pearson correlation) cross-correlation of signals v1 and v2 at a given lag.</summary>
        /// <remarks></remarks>
        /// <param name="lag"></param>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// </code>
        /// </example>
        let normalizedXCorr lag v1 v2 = 
            correlationOf Seq.pearson lag v1 v2

        /// <summary>computes the unnormalized (using only the dot product) cross-correlation of signals v1 and v2 at a given lag.</summary>
        /// <remarks></remarks>
        /// <param name="lag"></param>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// </code>
        /// </example>
        let xCorr lag v1 v2 = 
            correlationOf Vector.dot lag v1 v2

        /// <summary>Biweighted Midcorrelation. This is a median based correlation measure which is more robust against outliers.</summary>
        /// <remarks></remarks>
        /// <param name="vec1"></param>
        /// <param name="vec2"></param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// </code>
        /// </example>
        let bicor (vec1:vector) (vec2:vector) = 
            
            let xs,ys  = vec1.Values, vec2.Values

            let xMed = xs |> Array.median
            let xMad = xs |> Array.medianAbsoluteDev
            let xWeights = xs |> Array.map ((bicorHelpers.deviation xMed xMad) >> bicorHelpers.weight)
            let xNF = bicorHelpers.getNormalizationFactor xs xWeights xMed

            let yMed = ys |> Array.median
            let yMad = ys |> Array.medianAbsoluteDev
            let yWeights = ys |> Array.map ((bicorHelpers.deviation yMed yMad) >> bicorHelpers.weight)
            let yNF = bicorHelpers.getNormalizationFactor ys yWeights yMed

            bicorHelpers.sumBy4 (fun xVal xWeight yVal yWeight ->
                (bicorHelpers.normalize xVal xWeight xNF xMed) * (bicorHelpers.normalize yVal yWeight yNF yMed)
                )
                xs xWeights ys yWeights 

    /// Contains correlation functions optimized for matrices
    [<AutoOpen>]
    module Matrix =
    
        // Implemented according to the R package "MatrixCorrelation"
        /// <summary>Computes the rv2 coefficient.  </summary>
        /// <remarks></remarks>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// </code>
        /// </example>
        let rv2 (x: matrix) (y: matrix) =
            let xxt = x*x.Transpose 
            let yyt = y*y.Transpose 
            xxt |> Matrix.inplace_mapi (fun r c x -> if r = c then 0. else x)
            yyt |> Matrix.inplace_mapi (fun r c x -> if r = c then 0. else x)
            let num = (xxt*yyt).Diagonal |> Vector.sum
            let deno1 = xxt |> Matrix.map (fun x -> x**2.) |> Matrix.sum |> sqrt 
            let deno2 = yyt |> Matrix.map (fun x -> x**2.) |> Matrix.sum |> sqrt 
            num / (deno1 * deno2)
        
        /// <summary>computes a matrix that contains the metric given by the corrFunction parameter applied rowwise for every row against every other row of the input matrix</summary>
        /// <remarks></remarks>
        /// <param name="corrFunction"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// </code>
        /// </example>
        let rowWiseCorrelationMatrix (corrFunction : seq<float> -> seq<float> -> float) (m : matrix) =
            let vectors = Matrix.toJaggedArray m
            let result : float [] [] = [|for i=0 to vectors.Length-1 do yield (Array.init vectors.Length (fun innerIndex -> if i=innerIndex then 1. else 0.))|]
            for i=0 to vectors.Length-1 do
                for j=i+1 to vectors.Length-1 do
                    let corr = corrFunction vectors.[i] vectors.[j]
                    result.[i].[j] <- corr
                    result.[j].[i] <- corr
            result |> matrix

        /// <summary>computes a matrix that contains the metric given by the corrFunction parameter applied columnwise for every column against every other column of the input matrix</summary>
        /// <remarks></remarks>
        /// <param name="corrFunction"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// </code>
        /// </example>
        let columnWiseCorrelationMatrix (corrFunction : seq<float> -> seq<float> -> float) (m : Matrix<float>) =
            m
            |> Matrix.transpose
            |> (rowWiseCorrelationMatrix corrFunction)

        /// <summary>computes the rowwise pearson correlation matrix for the input matrix</summary>
        /// <remarks></remarks>
        /// <param name="m"></param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// </code>
        /// </example>
        let rowWisePearson (m:Matrix<float>) =
            m
            |> rowWiseCorrelationMatrix Seq.pearson

       /// <summary>computes the columnwise pearson correlation matrix for the input matrix</summary>
        /// <remarks></remarks>
        /// <param name="m"></param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// </code>
        /// </example>
        let columnWisePearson (m:Matrix<float>) =
            m
            |> columnWiseCorrelationMatrix Seq.pearson

        /// <summary>Computes the rowwise biweighted midcorrelation matrix for the input matrix </summary>
        /// <remarks></remarks>
        /// <param name="m"></param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// </code>
        /// </example>
        let rowWiseBicor (m : matrix) =

            let vectors = Matrix.toJaggedArray m
            let result : float [] [] = [|for i=0 to vectors.Length-1 do yield (Array.init vectors.Length (fun innerIndex -> if i=innerIndex then 1. else 0.))|]

            let meds : float [] = Array.zeroCreate vectors.Length
            let mads : float [] = Array.zeroCreate vectors.Length
            let weightss : float [][] = Array.zeroCreate vectors.Length
            let nfs : float [] = Array.zeroCreate vectors.Length

            for i=0 to vectors.Length-1 do
                let xs = vectors.[i]
                let xMed = xs |> Array.median
                let xMad = xs |> Array.medianAbsoluteDev
                let xWeights = xs |> Array.map ((bicorHelpers.deviation xMed xMad) >> bicorHelpers.weight)
                let xNF = bicorHelpers.getNormalizationFactor xs xWeights xMed

                meds.[i] <- xMed
                mads.[i] <- xMad
                weightss.[i] <- xWeights
                nfs.[i] <- xNF

                for j=0 to i - 1 do

                    let corr = 
                        bicorHelpers.sumBy4 (fun xVal xWeight yVal yWeight ->
                            (bicorHelpers.normalize xVal xWeight xNF xMed) * (bicorHelpers.normalize yVal yWeight nfs.[j] meds.[j]))
                            xs xWeights vectors.[j] weightss.[j]
                    result.[i].[j] <- corr
                    result.[j].[i] <- corr
            result |> matrix

        /// <summary>Computes the columnwise biweighted midcorrelation matrix for the input matrix </summary>
        /// <remarks></remarks>
        /// <param name="m"></param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// </code>
        /// </example>
        let columnWiseBicor (m : matrix) =
            m
            |> Matrix.transpose
            |> rowWiseBicor

        ///// Computes rowise pearson correlation
        //// TODO: TEST
        //let corr (x: matrix) =
        //    let exp = x |> Matrix.map (fun i -> i*i) |> Matrix.sumRows
        //    let z = exp * exp.Transpose |> Matrix.map sqrt
        //    let cov = x * x.Transpose
            
        //    cov ./ z

