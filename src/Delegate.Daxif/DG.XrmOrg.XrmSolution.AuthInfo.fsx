(**
AuthInfo
========

This script provides a way to prompt the user for a password upon using
one of the other scripts. It makes sure that the password is hidden and
sent along to the other scripts safely.
 
*)

let rk ()          = System.Console.ReadKey(true)
let cw (s:string)  = System.Console.Write(s)
let cwl (s:string) = System.Console.WriteLine(s)

(**

Function which ensures that when the password is entered it is 
hidden as a sequence of asterix.
*)
let mask key = 
  cw "-Enter pwd: "
  let rec mask' (acc : string) (k : System.ConsoleKeyInfo) = 
    match k.Key with
    | System.ConsoleKey.Enter -> 
      cwl System.String.Empty
      acc
    | System.ConsoleKey.Backspace -> 
      match acc.Length with
      | 0 -> mask' acc (rk())
      | _ -> 
        cw "\b \b"
        mask' (acc.[0..acc.Length - 2]) (rk())
    | _ -> 
      cw "*"
      mask' (acc + string (k.KeyChar)) (rk())
  mask' System.String.Empty (key())

(**

Finally the variables for user, password and domain are set.
*)
[<Literal>]
let usr = @"usr"
[<Literal>]
let domain = @"domain"
[<Literal>]
let pwd = @"pwd"

[<Literal>]
let usr' = @"usr"
[<Literal>]
let domain' = @"domain"
[<Literal>]
let pwd' = @"pwd"

(**
Alternatively use the following pwd value where you have to type the password
each time the script is executed. Remark: This can't be used with the 
TypeProvider module:

    let pwd = rk |> mask
*)
