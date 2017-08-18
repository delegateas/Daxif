module DG.Daxif.Modules.View.AllowedConditions

type NumberCondition =
  | Equal
  | NotEqual
  | GreaterThan
  | GreaterEqual
  | LessThan
  | LessEqual
  | HasData
  | NoData
  | Between
  | NotBetween
  | In
  | NotIn

type DateTimeCondition =
  | On
  | OnOrAfter
  | OnOrBefore
  | Yesterday
  | Today
  | Tomorrow
  | Next7Days
  | Last7Days
  | NextWeek
  | LastWeek
  | ThisWeek
  | NextMonth
  | LastMonth
  | ThisMonth
  | NextYear
  | LastYear
  | ThisYear
  | LastXHours
  | NextXHours
  | LastXDays
  | NextXDays
  | LastXMonths
  | NextXMonths
  | LastXYears
  | NextXYears
  | OlderThanXDays
  | OlderThanXHours
  | OlderThanXMinutes
  | OlderThanXMonths
  | OlderThanXWeeks
  | OlderThanXYears
  | AnyTime
  | HasData
  | NoData
  | InFiscalPeriod
  | InFiscalPeriodAndYear
  | InFiscalYear
  | InOrAfterFiscalPeriodAndYear
  | InOrBeforeFiscalPeriodAndYear
  | LastFiscalYear
  | NextFiscalYear
  | ThisFiscalYear
  | LastXFiscalYears
  | NextXFiscalYears
  | LastFiscalPeriod
  | NextFiscalPeriod
  | ThisFiscalPeriod
  | LastXFiscalPeriods
  | NextXFiscalPeriods

type GuidCondition =
  | Equal
  | NotEqual
  | HasData
  | NoData
  | EqualCurrentUser
  | DoesNotEqualCurrentuser
  | EqualCurrentUserOrReportingHierarchy
  | EqualCurrentUserOrReportingHierarchyAndTheirTeams
  | EqualCurrentUserTeams
  | EqualCurrentUserOrUserTeams
  | Under
  | NotUnder
  | UnderOrEqual
  | Above
  | AboveOrEqual
  | In
  | NotIn


type OptionCondition =
  | Equal
  | NotEqual
  | HasData
  | NoData
  | In
  | NotIn

type BoolCondition =
  | Equal
  | NotEqual
  | HasData
  | NoData
  | In
  | NotIn

type CollectionCondition =
  | Equal
  | NotEqual
  | HasData
  | NoData
  | In
  | NotIn

type StringCondition =
  | Equal
  | NotEqual
  | HasData
  | NoData
  | Contains
  | DoesNotContain 
  | BeginsWith
  | DoesNotBeginWith
  | EndsWith
  | DoesNotEndWith
  | In
  | NotIn

open Microsoft.Xrm.Sdk.Query
let parseCondition condition =
  let boxed = box condition
  match boxed with
  | :? StringCondition -> 
    match boxed :?> StringCondition with
    | StringCondition.Equal -> ConditionOperator.Equal
    | StringCondition.NotEqual -> ConditionOperator.NotEqual
    | StringCondition.HasData -> ConditionOperator.NotNull
    | StringCondition.NoData -> ConditionOperator.Null
    | StringCondition.Contains -> ConditionOperator.Contains
    | StringCondition.DoesNotContain -> ConditionOperator.DoesNotContain
    | StringCondition.BeginsWith -> ConditionOperator.BeginsWith
    | StringCondition.DoesNotBeginWith -> ConditionOperator.DoesNotBeginWith
    | StringCondition.EndsWith -> ConditionOperator.EndsWith
    | StringCondition.DoesNotEndWith -> ConditionOperator.DoesNotEndWith
    | StringCondition.In -> ConditionOperator.In
    | StringCondition.NotIn -> ConditionOperator.NotIn

  | :? OptionCondition ->
    match boxed :?> OptionCondition with
    | OptionCondition.Equal -> ConditionOperator.Equal
    | OptionCondition.NotEqual -> ConditionOperator.NotEqual
    | OptionCondition.HasData -> ConditionOperator.NotNull
    | OptionCondition.NoData -> ConditionOperator.Null
    | OptionCondition.In -> ConditionOperator.In
    | OptionCondition.NotIn -> ConditionOperator.NotIn

  | :? BoolCondition ->
    match boxed :?> BoolCondition with
    | BoolCondition.Equal -> ConditionOperator.Equal
    | BoolCondition.NotEqual -> ConditionOperator.NotEqual
    | BoolCondition.HasData -> ConditionOperator.NotNull
    | BoolCondition.NoData -> ConditionOperator.Null
    | BoolCondition.In -> ConditionOperator.In
    | BoolCondition.NotIn -> ConditionOperator.NotIn

  | :? GuidCondition ->
    match boxed :?> GuidCondition with
    | GuidCondition.Equal -> ConditionOperator.Equal
    | GuidCondition.NotEqual -> ConditionOperator.NotEqual
    | GuidCondition.HasData -> ConditionOperator.NotNull
    | GuidCondition.NoData -> ConditionOperator.Null
    | GuidCondition.EqualCurrentUser -> ConditionOperator.EqualUserId
    | GuidCondition.DoesNotEqualCurrentuser -> ConditionOperator.NotEqualUserId
    | GuidCondition.EqualCurrentUserOrReportingHierarchy -> ConditionOperator.EqualUserOrUserHierarchy
    | GuidCondition.EqualCurrentUserOrReportingHierarchyAndTheirTeams -> ConditionOperator.EqualUserOrUserHierarchyAndTeams
    | GuidCondition.EqualCurrentUserTeams -> ConditionOperator.EqualUserTeams
    | GuidCondition.EqualCurrentUserOrUserTeams -> ConditionOperator.EqualUserOrUserTeams
    | GuidCondition.Under -> ConditionOperator.Under
    | GuidCondition.NotUnder -> ConditionOperator.NotUnder
    | GuidCondition.In -> ConditionOperator.In
    | GuidCondition.NotIn -> ConditionOperator.NotIn
    | GuidCondition.UnderOrEqual -> ConditionOperator.UnderOrEqual
    | GuidCondition.Above -> ConditionOperator.Above
    | GuidCondition.AboveOrEqual -> ConditionOperator.AboveOrEqual

  | :? NumberCondition ->
    match boxed :?> NumberCondition with
    | NumberCondition.Equal -> ConditionOperator.Equal
    | NumberCondition.NotEqual -> ConditionOperator.NotEqual
    | NumberCondition.GreaterThan -> ConditionOperator.GreaterThan
    | NumberCondition.GreaterEqual -> ConditionOperator.GreaterEqual
    | NumberCondition.LessThan -> ConditionOperator.LessThan
    | NumberCondition.LessEqual -> ConditionOperator.LessEqual
    | NumberCondition.HasData -> ConditionOperator.NotNull
    | NumberCondition.NoData -> ConditionOperator.Null
    | NumberCondition.Between -> ConditionOperator.Between
    | NumberCondition.NotBetween -> ConditionOperator.NotBetween
    | NumberCondition.In -> ConditionOperator.In
    | NumberCondition.NotIn -> ConditionOperator.NotIn

  | :? DateTimeCondition ->
    match boxed :?> DateTimeCondition with
    | DateTimeCondition.On -> ConditionOperator.On
    | DateTimeCondition.OnOrAfter -> ConditionOperator.OnOrAfter
    | DateTimeCondition.OnOrBefore -> ConditionOperator.OnOrBefore
    | DateTimeCondition.Yesterday -> ConditionOperator.Yesterday
    | DateTimeCondition.Today -> ConditionOperator.Today
    | DateTimeCondition.Tomorrow -> ConditionOperator.Tomorrow
    | DateTimeCondition.Next7Days -> ConditionOperator.Next7Days
    | DateTimeCondition.Last7Days -> ConditionOperator.Last7Days
    | DateTimeCondition.NextWeek -> ConditionOperator.NextWeek
    | DateTimeCondition.LastWeek -> ConditionOperator.LastWeek
    | DateTimeCondition.ThisWeek -> ConditionOperator.ThisWeek
    | DateTimeCondition.NextMonth -> ConditionOperator.NextMonth
    | DateTimeCondition.LastMonth -> ConditionOperator.LastMonth
    | DateTimeCondition.ThisMonth -> ConditionOperator.ThisMonth
    | DateTimeCondition.NextYear -> ConditionOperator.NextYear
    | DateTimeCondition.LastYear -> ConditionOperator.LastYear
    | DateTimeCondition.ThisYear -> ConditionOperator.ThisYear
    | DateTimeCondition.LastXHours -> ConditionOperator.LastXHours
    | DateTimeCondition.NextXHours -> ConditionOperator.NextXHours
    | DateTimeCondition.LastXDays -> ConditionOperator.LastXDays
    | DateTimeCondition.NextXDays -> ConditionOperator.NextXDays
    | DateTimeCondition.LastXMonths -> ConditionOperator.LastXMonths
    | DateTimeCondition.NextXMonths -> ConditionOperator.NextXMonths
    | DateTimeCondition.LastXYears -> ConditionOperator.LastXYears
    | DateTimeCondition.NextXYears -> ConditionOperator.NextXYears
    | DateTimeCondition.OlderThanXDays -> ConditionOperator.OlderThanXDays
    | DateTimeCondition.OlderThanXHours -> ConditionOperator.OlderThanXHours
    | DateTimeCondition.OlderThanXMinutes -> ConditionOperator.OlderThanXMinutes
    | DateTimeCondition.OlderThanXMonths -> ConditionOperator.OlderThanXMonths
    | DateTimeCondition.OlderThanXWeeks -> ConditionOperator.OlderThanXWeeks
    | DateTimeCondition.OlderThanXYears -> ConditionOperator.OlderThanXYears
    | DateTimeCondition.AnyTime -> ConditionOperator.NotNull
    | DateTimeCondition.HasData -> ConditionOperator.NotNull
    | DateTimeCondition.NoData -> ConditionOperator.Null
    | DateTimeCondition.InFiscalPeriod -> ConditionOperator.InFiscalPeriod
    | DateTimeCondition.InFiscalPeriodAndYear -> ConditionOperator.InFiscalPeriodAndYear
    | DateTimeCondition.InFiscalYear -> ConditionOperator.InFiscalYear
    | DateTimeCondition.InOrAfterFiscalPeriodAndYear -> ConditionOperator.InOrAfterFiscalPeriodAndYear
    | DateTimeCondition.InOrBeforeFiscalPeriodAndYear -> ConditionOperator.InOrBeforeFiscalPeriodAndYear
    | DateTimeCondition.LastFiscalYear -> ConditionOperator.LastFiscalYear
    | DateTimeCondition.NextFiscalYear -> ConditionOperator.NextFiscalYear
    | DateTimeCondition.ThisFiscalYear -> ConditionOperator.ThisFiscalYear
    | DateTimeCondition.LastXFiscalYears -> ConditionOperator.LastXFiscalYears
    | DateTimeCondition.NextXFiscalYears -> ConditionOperator.NextXFiscalYears
    | DateTimeCondition.LastFiscalPeriod -> ConditionOperator.LastFiscalPeriod
    | DateTimeCondition.NextFiscalPeriod -> ConditionOperator.NextFiscalPeriod
    | DateTimeCondition.ThisFiscalPeriod -> ConditionOperator.ThisFiscalPeriod
    | DateTimeCondition.LastXFiscalPeriods -> ConditionOperator.LastXFiscalPeriods
    | DateTimeCondition.NextXFiscalPeriods -> ConditionOperator.NextXFiscalPeriods

  | _ -> failwith "unkown condition in condition parser, create a new condition for the type"