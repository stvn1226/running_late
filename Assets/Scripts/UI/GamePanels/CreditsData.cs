using UnityEngine;
using System;
using System.Collections.Generic;

public enum ContributorType
{
    CoreTeam,
    Freelancer,
    Backer,
    SpecialThanks,
    Tooling
}

public enum Department
{
    Management,
    Programming,
    Design,
    Art,
    Animation,
    Audio,
    QA,
    Marketing,
    Community
}

public enum Seniority
{
    Director = 0,
    Lead = 1,
    Senior = 2,
    Mid = 3,
    Junior = 4,
    Intern = 5,
    Contributor = 6
}

[Serializable]
public class Role
{
    public Department department = Department.Programming;
    public Seniority seniority = Seniority.Mid;
    
    [Tooltip("Leave empty to use auto-generated title")]
    public string customRoleTitle = "";
    
    public string GetFormattedTitle()
    {
        if (!string.IsNullOrEmpty(customRoleTitle))
        {
            return customRoleTitle;
        }
        
        string seniorityStr = GetSeniorityString();
        string departmentStr = GetDepartmentString();
        
        if (string.IsNullOrEmpty(seniorityStr))
        {
            return departmentStr;
        }
        
        return $"{seniorityStr} {departmentStr}";
    }
    
    private string GetSeniorityString()
    {
        switch (seniority)
        {
            case Seniority.Director:
                return "Director";
            case Seniority.Lead:
                return "Lead";
            case Seniority.Senior:
                return "Senior";
            case Seniority.Mid:
                return "";
            case Seniority.Junior:
                return "Junior";
            case Seniority.Intern:
                return "Intern";
            case Seniority.Contributor:
                return "Contributor";
            default:
                return "";
        }
    }
    
    private string GetDepartmentString()
    {
        switch (department)
        {
            case Department.Management:
                return "Management";
            case Department.Programming:
                return "Programmer";
            case Department.Design:
                return "Designer";
            case Department.Art:
                return "Artist";
            case Department.Animation:
                return "Animator";
            case Department.Audio:
                return "Audio Designer";
            case Department.QA:
                return "QA Tester";
            case Department.Marketing:
                return "Marketing";
            case Department.Community:
                return "Community Manager";
            default:
                return "";
        }
    }
    
    public int GetSortPriority()
    {
        int deptOrder = (int)department * 1000;
        int seniorityOrder = (int)seniority * 100;
        return deptOrder + seniorityOrder;
    }
}

