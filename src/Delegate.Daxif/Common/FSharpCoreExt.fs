namespace DG.Daxif.Common

module internal FSharpCoreExt = 
  module internal Array = 
    /// Array must be sorted
    let binExists v a = (System.Array.BinarySearch(a, v) >= 0)
    
    let chunk size (source:'a[]) = 
      match source.Length <= size with
      | true -> [|source|]
      | false ->
        [|
          let r = ResizeArray()
          for x in source do
            r.Add(x)
            if r.Count = size then 
              yield r.ToArray()
              r.Clear()
          if r.Count > 0 then yield r.ToArray()
        |]

  module internal Seq = 
    let split size source = 
      match Seq.length source <= size with
      | true -> seq{ yield source |> Array.ofSeq}
      | false ->
        seq { 
          let r = ResizeArray()
          for x in source do
            r.Add(x)
            if r.Count = size then 
              yield r.ToArray()
              r.Clear()
          if r.Count > 0 then yield r.ToArray()
        }
