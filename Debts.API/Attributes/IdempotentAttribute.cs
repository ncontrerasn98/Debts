namespace Debts.API.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class IdempotentAttribute : Attribute { }