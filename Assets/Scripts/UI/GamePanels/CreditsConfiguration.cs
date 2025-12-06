using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "CreditsConfiguration", menuName = "Running Late/Credits/Credits Configuration")]
public class CreditsConfiguration : ScriptableObject
{
    [Header("Studio Information")]
    public string studioName = "Unbreakable Interactive";
    public string gameName = "Running Late";
    public string copyrightYear = "2024";
    
    [Header("Team Members")]
    public List<TeamMember> allMembers = new List<TeamMember>();
    
    [Header("Custom Sections")]
    [TextArea(3, 10)]
    public string additionalCredits = "";
    
    [Header("Display Settings")]
    public bool showSocialHandles = true;
    public bool groupByDepartment = true;
    public bool groupByContributorType = true;
    
    [Tooltip("When a person has multiple roles, list them all instead of grouping by department")]
    public bool showAllRolesInline = true;
    
    [Header("Text Sizes")]
    public int studioBannerSize = 70;
    public int gameNameSize = 60;
    public int sectionTitleSize = 40;
    public int subsectionTitleSize = 30;
    public int nameSize = 24;
    public int roleSize = 20;
    public int socialSize = 16;
    
    public string GenerateCreditsText()
    {
        string credits = "";
        
        credits += $"<align=center><size={studioBannerSize}><b>{studioName.ToUpper()}</b></size>\n";
        credits += $"<size={gameNameSize - 10}>presents</size>\n\n";
        credits += $"<size={gameNameSize}><b>{gameName.ToUpper()}</b></size>\n\n\n";
        
        if (groupByContributorType)
        {
            credits += GenerateCoreTeamSection();
            credits += GenerateFreelancerSection();
            credits += GenerateBackersSection();
            credits += GenerateSpecialThanksSection();
        }
        else
        {
            credits += GenerateAllMembersFlat();
        }
        
        credits += GenerateToolingSection();
        
        if (!string.IsNullOrEmpty(additionalCredits))
        {
            credits += $"\n\n{additionalCredits}\n\n";
        }
        
        credits += $"\n\n<size={gameNameSize}><b>THANK YOU FOR PLAYING!</b></size>\n\n";
        credits += $"<size={nameSize}>© {copyrightYear} {studioName}</size>\n";
        credits += $"<size={roleSize}>All Rights Reserved</size>\n\n";
        credits += $"<size={socialSize}>Made with ❤️ and dedication</size>\n";
        credits += "</align>";
        
        return credits;
    }
    
    private string GenerateCoreTeamSection()
    {
        var coreTeam = allMembers
            .Where(m => m.contributorType == ContributorType.CoreTeam)
            .OrderBy(m => m.GetPrimarySortOrder())
            .ToList();
        
        if (coreTeam.Count == 0) return "";
        
        string section = $"<size={sectionTitleSize}><b>CORE TEAM</b></size>\n\n";
        
        if (groupByDepartment && !showAllRolesInline)
        {
            HashSet<TeamMember> processedMembers = new HashSet<TeamMember>();
            
            foreach (Department dept in System.Enum.GetValues(typeof(Department)))
            {
                var membersInDept = coreTeam
                    .Where(m => m.HasDepartment(dept) && !processedMembers.Contains(m))
                    .OrderBy(m => m.GetHighestSeniorityInDepartment(dept))
                    .ThenBy(m => m.displayOrder)
                    .ToList();
                
                if (membersInDept.Count > 0)
                {
                    section += $"<size={subsectionTitleSize}><b>{GetDepartmentTitle(dept)}</b></size>\n";
                    
                    foreach (var member in membersInDept)
                    {
                        var rolesInDept = member.GetRolesInDepartment(dept);
                        section += FormatMemberWithSpecificRoles(member, rolesInDept);
                        processedMembers.Add(member);
                    }
                    
                    section += "\n";
                }
            }
        }
        else
        {
            foreach (var member in coreTeam)
            {
                section += FormatMember(member);
            }
        }
        
        return section + "\n";
    }
    
    private string GenerateFreelancerSection()
    {
        var freelancers = allMembers
            .Where(m => m.contributorType == ContributorType.Freelancer)
            .OrderBy(m => m.GetPrimarySortOrder())
            .ToList();
        
        if (freelancers.Count == 0) return "";
        
        string section = $"<size={sectionTitleSize}><b>FREELANCE CONTRIBUTORS</b></size>\n\n";
        
        if (groupByDepartment && !showAllRolesInline)
        {
            HashSet<TeamMember> processedMembers = new HashSet<TeamMember>();
            
            foreach (Department dept in System.Enum.GetValues(typeof(Department)))
            {
                var membersInDept = freelancers
                    .Where(m => m.HasDepartment(dept) && !processedMembers.Contains(m))
                    .OrderBy(m => m.displayOrder)
                    .ToList();
                
                if (membersInDept.Count > 0)
                {
                    section += $"<size={subsectionTitleSize}><b>{GetDepartmentTitle(dept)}</b></size>\n";
                    
                    foreach (var member in membersInDept)
                    {
                        var rolesInDept = member.GetRolesInDepartment(dept);
                        section += FormatMemberWithSpecificRoles(member, rolesInDept);
                        processedMembers.Add(member);
                    }
                    
                    section += "\n";
                }
            }
        }
        else
        {
            foreach (var member in freelancers)
            {
                section += FormatMember(member);
            }
        }
        
        return section + "\n";
    }
    
    private string GenerateBackersSection()
    {
        var backers = allMembers
            .Where(m => m.contributorType == ContributorType.Backer)
            .OrderBy(m => m.displayOrder)
            .ThenBy(m => m.displayName)
            .ToList();
        
        if (backers.Count == 0) return "";
        
        string section = $"<size={sectionTitleSize}><b>SPECIAL THANKS TO OUR BACKERS</b></size>\n\n";
        
        foreach (var backer in backers)
        {
            section += $"<size={nameSize}>{backer.displayName}</size>\n";
            
            if (!string.IsNullOrEmpty(backer.additionalCredit))
            {
                section += $"<size={socialSize}><i>{backer.additionalCredit}</i></size>\n";
            }
            
            if (showSocialHandles && !string.IsNullOrEmpty(backer.socialHandle))
            {
                section += $"<size={socialSize}><i>{backer.socialHandle}</i></size>\n";
            }
            
            section += "\n";
        }
        
        return section + "\n";
    }
    
    private string GenerateSpecialThanksSection()
    {
        var specialThanks = allMembers
            .Where(m => m.contributorType == ContributorType.SpecialThanks)
            .OrderBy(m => m.displayOrder)
            .ToList();
        
        if (specialThanks.Count == 0) return "";
        
        string section = $"<size={sectionTitleSize}><b>SPECIAL THANKS</b></size>\n\n";
        
        foreach (var person in specialThanks)
        {
            section += $"<size={nameSize}>{person.displayName}</size>\n";
            
            if (!string.IsNullOrEmpty(person.additionalCredit))
            {
                section += $"<size={roleSize}><i>{person.additionalCredit}</i></size>\n";
            }
            
            if (showSocialHandles && !string.IsNullOrEmpty(person.socialHandle))
            {
                section += $"<size={socialSize}><i>{person.socialHandle}</i></size>\n";
            }
            
            section += "\n";
        }
        
        return section;
    }
    
    private string GenerateToolingSection()
    {
        var tools = allMembers
            .Where(m => m.contributorType == ContributorType.Tooling)
            .OrderBy(m => m.displayOrder)
            .ToList();
        
        string section = $"\n<size={sectionTitleSize}><b>TOOLS & MIDDLEWARE</b></size>\n\n";
        
        section += "<size=" + nameSize + ">Unity Engine\n";
        section += "Universal Render Pipeline (URP)\n";
        section += "Cinemachine\n";
        section += "New Input System\n";
        section += "2D Animation & Sprite Tools\n";
        section += "Feel (MoreMountains Feedbacks)\n";
        section += "TextAnimator (Febucci)</size>\n\n";
        
        foreach (var tool in tools)
        {
            section += $"<size={nameSize}>{tool.displayName}</size>\n";
            
            if (!string.IsNullOrEmpty(tool.additionalCredit))
            {
                section += $"<size={roleSize}><i>{tool.additionalCredit}</i></size>\n";
            }
        }
        
        return section;
    }
    
    private string GenerateAllMembersFlat()
    {
        string section = "";
        
        foreach (var member in allMembers.OrderBy(m => m.GetPrimarySortOrder()))
        {
            section += FormatMember(member);
        }
        
        return section;
    }
    
    private string FormatMember(TeamMember member)
    {
        string formatted = $"<size={nameSize}><b>{member.displayName}</b></size>\n";
        formatted += $"<size={roleSize}>{member.GetFormattedRoles()}</size>\n";
        
        if (showSocialHandles && !string.IsNullOrEmpty(member.socialHandle))
        {
            formatted += $"<size={socialSize}><i>{member.socialHandle}</i></size>\n";
        }
        
        if (!string.IsNullOrEmpty(member.additionalCredit))
        {
            formatted += $"<size={socialSize}><i>{member.additionalCredit}</i></size>\n";
        }
        
        formatted += "\n";
        
        return formatted;
    }
    
    private string FormatMemberWithSpecificRoles(TeamMember member, List<Role> specificRoles)
    {
        string formatted = $"<size={nameSize}><b>{member.displayName}</b></size>\n";
        
        List<string> roleTitles = new List<string>();
        foreach (var role in specificRoles)
        {
            roleTitles.Add(role.GetFormattedTitle());
        }
        
        formatted += $"<size={roleSize}>{string.Join(" • ", roleTitles)}</size>\n";
        
        if (showSocialHandles && !string.IsNullOrEmpty(member.socialHandle))
        {
            formatted += $"<size={socialSize}><i>{member.socialHandle}</i></size>\n";
        }
        
        if (!string.IsNullOrEmpty(member.additionalCredit))
        {
            formatted += $"<size={socialSize}><i>{member.additionalCredit}</i></size>\n";
        }
        
        formatted += "\n";
        
        return formatted;
    }
    
    private string GetDepartmentTitle(Department dept)
    {
        switch (dept)
        {
            case Department.Management: return "Management";
            case Department.Programming: return "Programming";
            case Department.Design: return "Game Design";
            case Department.Art: return "Art";
            case Department.Animation: return "Animation";
            case Department.Audio: return "Audio";
            case Department.QA: return "Quality Assurance";
            case Department.Marketing: return "Marketing";
            case Department.Community: return "Community";
            default: return dept.ToString();
        }
    }
}
