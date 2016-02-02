namespace DG.Daxif.HelperModules.Common

module internal FSharpCoreExt = 
  module internal Array = 
    /// Array must be sorted
    let binExists v a = (System.Array.BinarySearch(a, v) >= 0)

  module internal Seq = 
    let split size source = 
      seq { 
        let r = ResizeArray()
        for x in source do
          r.Add(x)
          if r.Count = size then 
            yield r.ToArray()
            r.Clear()
        if r.Count > 0 then yield r.ToArray()
      }
