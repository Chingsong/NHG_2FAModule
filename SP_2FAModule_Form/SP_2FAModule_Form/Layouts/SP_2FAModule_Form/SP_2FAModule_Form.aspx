<%@ Assembly Name="$SharePoint.Project.AssemblyFullName$" %>
<%@ Import Namespace="Microsoft.SharePoint.ApplicationPages" %>
<%@ Register TagPrefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register TagPrefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register TagPrefix="asp" Namespace="System.Web.UI" Assembly="System.Web.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" %>
<%@ Import Namespace="Microsoft.SharePoint" %>
<%@ Assembly Name="Microsoft.Web.CommandUI, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="SP_2FAModule_Form.aspx.cs" Inherits="SP_2FAModule_Form.Layouts.SP_2FAModule_Form.SP_2FAModule_Form" DynamicMasterPageFile="~masterurl/default.master" %>

<asp:Content ID="PageHead" ContentPlaceHolderID="PlaceHolderAdditionalPageHead" runat="server">
</asp:Content>

<asp:Content ID="Main" ContentPlaceHolderID="PlaceHolderMain" runat="server">
    <div id="FormDiv" runat="server" style="background-color: aliceblue; width: 450px!important; height: 500px!important; margin: auto; border: 3px solid #004d85; padding: 10px;">
        <div id="FormContentHolder" style="display: inline-grid; font-size: 16px">
            <div id="FormTitle" style="margin-top: 25px; display: flex;">
                <div style="margin: 10px 25px;">
                    <asp:Image ID="Image1" runat="server" ImageUrl="../images/NHG_logo.jpg" />
                </div>
                <div style="margin-block: auto; margin-right: auto;">
                    <asp:Label ID="Label1" runat="server" Text="NHG 2FA" Style="font-size: 30px; font-weight: bolder"></asp:Label>
                </div>
            </div>
            <div id="FormHeader" style="position: relative; display: inline-grid; text-align: center; margin-top: 50px">
                <div style="padding:5px">
                    <asp:Label ID="Lbl_Welcome" runat="server"></asp:Label>
                </div>
                
                <div style="padding:5px">
                    <%-- This component (UpdatePanel) is designed to be updated with AJAX and is where we store the info that changes --%>
                    <asp:UpdatePanel ID="UpdatePanel" runat="server" UpdateMode="Always">
                        <Triggers>
                            <asp:AsyncPostBackTrigger ControlID="Timer" EventName="Tick" />
                        </Triggers>
                        <ContentTemplate>
                            <asp:Label ID="DisplayOtpMsg" runat="server" Text="OTP expiring in "></asp:Label>
                            <span>:</span>
                            <asp:Label ID="DisplayTextSeconds" runat="server" Text="Seconds Left">00</asp:Label>
                            <span>seconds</span>
                        </ContentTemplate>
                    </asp:UpdatePanel>

                    <%-- This is the component that handles the timing event client-side --%>
                    <asp:Timer ID="Timer" runat="server" Interval="1000" OnTick="Timer_Tick" Enabled="false"></asp:Timer>
                </div>
            </div>
            <div id="FormContents" style="margin: 50px">
                <asp:Table BorderStyle="None" BorderColor="Black" runat="server"
                    CellPadding="10"
                    GridLines="Both"
                    HorizontalAlign="Center" Width="326px">
                    <asp:TableRow ID="TblRow_SessionCheckbox">
                        <asp:TableCell ColumnSpan="2">
                            <asp:CheckBox ID="Cb_Session" runat="server" Text="Another Session exist for same id, please select to disable and continue." />
                        </asp:TableCell>
                    </asp:TableRow>
                    <asp:TableRow>
                        <asp:TableCell ColumnSpan="2" Style="text-align: center">
                            <asp:TextBox ID="Txt_Otp" runat="server" Style="width: 90%"></asp:TextBox>
                        </asp:TableCell>
                    </asp:TableRow>
                    <asp:TableRow>
                        <asp:TableCell ColumnSpan="2" ID="TblCell_ErrorMsg_InvalidOtp" Style="text-align: center; display: none">
                            <asp:Label ID="Lbl_ErrorMsg_InvalidOtp" runat="server" Style="color: red;"></asp:Label>
                        </asp:TableCell>
                    </asp:TableRow>
                    <asp:TableRow>
                        <asp:TableCell ColumnSpan="2" ID="TblCell_ErrorMsg_MaxAttempt" Style="text-align: center; display: none">
                            <asp:Label ID="Lbl_ErrorMsg_MaxAttempt" runat="server" Style="color: red;"></asp:Label>
                        </asp:TableCell>
                    </asp:TableRow>
                    <asp:TableRow>
                        <asp:TableCell Style="text-align: center">
                            <asp:Button ID="Btn_Resend" runat="server" Text="Resend OTP" OnClick="Btn_Resend_Click" />
                        </asp:TableCell>
                        <asp:TableCell ID="TblCell_Submit" runat="server" Style="text-align: center">
                            <asp:Button ID="Btn_Submit" runat="server" Text="Submit SMS" OnClick="Btn_Submit_Click" />
                        </asp:TableCell>
                    </asp:TableRow>
                </asp:Table>
            </div>
        </div>
    </div>
</asp:Content>

<asp:Content ID="PageTitle" ContentPlaceHolderID="PlaceHolderPageTitle" runat="server">
    2FA Authentication Form
</asp:Content>

<asp:Content ID="PageTitleInTitleArea" ContentPlaceHolderID="PlaceHolderPageTitleInTitleArea" runat="server">
    2FA Authentication Form
</asp:Content>
