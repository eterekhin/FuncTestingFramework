module FuncTestingFramework.Generator
open System
open System.Collections
open System.Linq

type TupleElementTypes = Type array

[<RequireQualifiedAccess>]
module generator =
    let newNextArgs i j = Random().Next(i, j)
    let _int_32() = Random().Next()
    let _int_64(): int64 = int64 (_int_32()) * int64 (_int_32())
    let rec intervalInt64 (min: int64) (max: int64): int64 =
        if (min < 0L && max > Int64.MaxValue + min)
        then if Random().Next() % 2 = 0 then intervalInt64 min 0L else intervalInt64 0L max
        else int64 (double (max - min) * (Random().NextDouble())) + min
    let char() = Convert.ToChar (if _int_32() % 2 = 0 then newNextArgs 65 90 else newNextArgs 97 122)
    let string() = seq { for _ in 0..36 -> char() } |> String.Concat
    let bool() = _int_32() % 2 = 0
    let _decimal() = decimal (_int_32()) * decimal (Random().NextDouble())
    let rec generateDecimal (min: decimal) (max: decimal): decimal =
        if (min < 0M && max > Decimal.MaxValue + min)
        then if Random().Next() % 2 = 0 then generateDecimal 0M max else generateDecimal min 0M
        else (max - min) * (decimal (Random().NextDouble())) + min
    let date() = DateTime(intervalInt64 DateTime.MinValue.Ticks DateTime.MaxValue.Ticks)

[<RequireQualifiedAccess>]
module FunctionTester =
    type PrimitiveType =
        | Int32'
        | Int64'
        | String'
        | DateTime'
        | Decimal'
        | Boolean'
        | Class
        | IEnumerableInt
        | IEnumerableDateTime
        | IEnumerableDecimal
        | IEnumerableClass of Type
        | IEnumerableString
        | IEnumerableBool

   
    let rec (|GetType|) type' =
            match type' with
            | t when t = typeof<Int32> -> Int32'
            | t when t = typeof<Int64> -> Int64'
            | t when t = typeof<string> -> String'
            | t when t = typeof<DateTime> -> DateTime'
            | t when t = typeof<Decimal> -> Decimal'
            | t when t = typeof<bool> -> Boolean'
            | t when t.IsGenericType && t.GetInterfaces() |> Seq.exists (fun inf -> inf = typeof<IEnumerable>) ->
                let generic = t |> (fun x -> x.GenericTypeArguments |> Array.head)
                match generic with
                | GetType Int32' -> IEnumerableInt
                | GetType DateTime' -> IEnumerableDateTime
                | GetType Decimal' -> IEnumerableDecimal
                | GetType Class -> IEnumerableClass generic
                | GetType String' -> IEnumerableString
                | GetType Boolean' -> IEnumerableBool
                | _ -> failwith "Not Supported"

            | t when t.IsClass -> Class
            | _ -> failwith "Not Supported"

    let enumerableCast toType (enumerable: IEnumerable) =
        typeof<Enumerable>.GetMethod("Cast").MakeGenericMethod([|toType|]).Invoke(null, [| enumerable |])
    
    let rec generateByType (tp: Type) =
        match tp with
        | GetType Int32' -> (generator._int_32()) :> obj
        | GetType Int64' -> generator._int_64() :> obj
        | GetType String' -> generator.string() :> obj
        | GetType DateTime' -> generator.date() :> obj
        | GetType Boolean' -> generator.bool() :> obj
        | GetType Decimal' -> generator._decimal() :> obj
        | GetType IEnumerableInt -> Seq.init 100 (fun _ -> generateByType typeof<int> :?> int) :> obj
        | GetType IEnumerableDecimal -> Seq.init 100 (fun _ -> generateByType typeof<decimal> :?> decimal) :> obj
        | GetType IEnumerableDateTime -> Seq.init 100 (fun _ -> generateByType typeof<DateTime> :?> DateTime) :> obj
        | GetType IEnumerableString -> Seq.init 100 (fun _ -> generateByType typeof<string> :?> string) :> obj
        | GetType(IEnumerableClass classType) -> enumerableCast classType (Seq.init 100 (fun _ -> generateByType classType))
        | GetType(IEnumerableBool) -> Seq.init 100 (fun _ -> generateByType typeof<bool>) :> obj
        | GetType Class ->
            let new' = Activator.CreateInstance(tp)
            new'.GetType().GetProperties()
            |> Seq.iter (fun p ->
                let newValue = generateByType p.PropertyType
                p.SetValue(new', newValue))
            new'

