using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ManagerOrderAttribute : Attribute
{
    public int Order { get; }

    public ManagerOrderAttribute(int order)
    {
        this.Order = order;
    }
}
