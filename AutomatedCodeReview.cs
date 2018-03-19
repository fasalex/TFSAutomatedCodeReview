using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Controls.Extensibility;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace AutomatedCodeReview
{
    [TeamExplorerSection(SectionId, TeamExplorerPageIds.PendingChanges, 2)]
    public class AutomatedCodeReview : TeamExplorerSection
    {
        private const string SectionId = "EFD86431-C734-4956-9E46-1DFff4506778";

        public AutomatedCodeReview()
        {
            Title = @"Generate Code Review Requests At Checkin";
            IsVisible = true;
            IsExpanded = true;
            SectionContent = new AutomatedCodeReviewView();
        }

        public override void Loaded(object sender, SectionLoadedEventArgs e)
        {
            TfsTeamProjectCollection teamProjectCollection = CurrentContext.TeamProjectCollection;
            VersionControlServer versionControl = teamProjectCollection?.GetService<VersionControlServer>();
            if (versionControl == null)
            {
                return;
            }
            versionControl.BeforeCheckinPendingChange += OnChekinPendingChanges;
        }

        private void OnChekinPendingChanges(object sender, ProcessingChangeEventArgs processingChangeEvent)
        {
            AutomatedCodeReviewView view = SectionContent as AutomatedCodeReviewView;

            if (view == null || !view.IsChecked)
            {
                return;
            }
            Workspace workspace = processingChangeEvent.Workspace;
            VersionControlServer control = workspace.VersionControlServer;
            ITeamFoundationContext currentContext = CurrentContext;

            if (control == null  || currentContext == null)
            {
                return;
            }

            IPendingChangesExt pendingChangesExtService = GetService<IPendingChangesExt>();

            //Create the code review request once, as the event is fired for every objects to be checked in.

            PendingChange[] pendingChanges = pendingChangesExtService.IncludedChanges;
            PendingChange pendingChange = pendingChanges[0];
            if (pendingChange.ItemId != processingChangeEvent.PendingChange.ItemId)
            {
                return;
            }
     
            WorkItemStore workitemStore = currentContext.TeamProjectCollection.GetService<WorkItemStore>();
            Project project = workitemStore.Projects[currentContext.TeamProjectName];


            //Create the shelve set 

            DateTime dateTimeNow = DateTime.UtcNow;
            Shelveset shelveSet = new Shelveset(control, 
                                                $@"Code Review_{dateTimeNow.Year}-{dateTimeNow.Month}-{dateTimeNow.Day}_{dateTimeNow.Hour}.{dateTimeNow.Minute}.{dateTimeNow.Second}.{dateTimeNow.Millisecond}",
                                                workspace.OwnerName);

            shelveSet.WorkItemInfo = pendingChangesExtService.WorkItems; 
            workspace.Shelve(shelveSet, pendingChanges, ShelvingOptions.None);
            
            //Create the review request 

            WorkItemType type = project.WorkItemTypes["Code Review Request"];
            WorkItem workItem = new WorkItem(type) { Title = workspace.Comment };
            workItem.Fields["System.AssignedTo"].Value = control.AuthenticatedUser;
            workItem.Fields["Microsoft.VSTS.CodeReview.Context"].Value = shelveSet.Name;
            workItem.Fields["System.AreaPath"].Value = currentContext.TeamProjectName;
            workItem.Fields["System.IterationPath"].Value = project.Name;
            workItem.Fields["System.State"].Value = "Requested";
            workItem.Fields["System.Reason"].Value = "New";
            workItem.Fields["System.Description"].Value = $"Code Review Request from {control.AuthenticatedUser}";
            workItem.Fields["System.Title"].Value = pendingChangesExtService.CheckinComment;

            //Get the list of recent reviewers 
            string workItemQuery =
                $@"SELECT * FROM WorkItems WHERE [System.TeamProject] = '{project.Name}' AND [Work Item Type] = 'Code Review Request' AND [Area Path] = '{project.Name}' AND [Created By] = '{control.AuthenticatedUser}' AND [System.State] <> 'Closed' ORDER BY [System.CreatedDate]";

            WorkItemCollection allReviewItems = workitemStore.Query(workItemQuery);
            WorkItem lastItem = allReviewItems.OfType<WorkItem>().LastOrDefault();

            IEnumerable<int> recentReviewers = lastItem?.Links.OfType<RelatedLink>().Select(x => x.RelatedWorkItemId);
            if (recentReviewers != null)
            {
                foreach (int recentReviewer in recentReviewers)
                {
                    //Create Code Review Response for each reviewer 

                    WorkItem oldCodeReviewResponse = workitemStore.GetWorkItem(recentReviewer);
                    WorkItemType codeReviewResponseType = project.WorkItemTypes["Code Review Response"];
                    WorkItem codeReviewResponseItem = new WorkItem(codeReviewResponseType) {Title = @"Code Review Response"};
                    codeReviewResponseItem.Fields["System.AssignedTo"].Value = oldCodeReviewResponse.Fields["System.AssignedTo"].Value;
                    codeReviewResponseItem.Fields["System.State"].Value = "Requested";
                    codeReviewResponseItem.Fields["System.Reason"].Value = "New";
                    ArrayList result = codeReviewResponseItem.Validate();
                    if (result.Count != 0)
                    {
                        continue;
                    }
                    codeReviewResponseItem.Save();
                    int responseId = codeReviewResponseItem.Id;
                    WorkItemLinkTypeEnd linkTypeEnd = workitemStore.WorkItemLinkTypes.LinkTypeEnds["Child"];
                    workItem.Links.Add(new RelatedLink(linkTypeEnd, responseId));
                }
            }

            ArrayList codeReviewRequestResult = workItem.Validate();
            if (codeReviewRequestResult.Count != 0)
            {
                return;
            }
            workItem.Save();
        }
    }
}