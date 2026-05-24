using System;

namespace SimpleModule.Core.FormRequests;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class FormRequestAttribute : Attribute { }
