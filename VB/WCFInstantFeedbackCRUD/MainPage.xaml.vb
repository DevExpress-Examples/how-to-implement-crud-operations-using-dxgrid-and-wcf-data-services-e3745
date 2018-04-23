Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.Data.Services.Client
Imports System.Linq
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Threading
Imports DevExpress.Xpf.Core
Imports DevExpress.Xpf.Core.ServerMode
Imports DevExpress.Xpf.Grid
Imports WCFInstantFeedbackCRUD.NorthwindService

Namespace WCFInstantFeedbackCRUD
	Partial Public Class MainPage
		Inherits UserControl
		Private context As NorthwindEntities
		Private control As Control
		Private newCustomer As Customers
		Private customerToEdit As Customers
                                Private uiDispatcher As Dispatcher

		Public Sub New()
			InitializeComponent()
                                                uiDispatcher = grid.Dispatcher
			context = New NorthwindEntities(New Uri("NorthwindService.svc/", UriKind.Relative))
			wcfInstantSource.DataServiceContext = context
			wcfInstantSource.Query = context.Customers
		End Sub

		Private Sub Add_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			newCustomer = CreateNewCustomer()
			EditCustomer(newCustomer, "Add new customer", AddressOf CloseAddNewCustomerHandler)
		End Sub
		Private Sub Remove_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			DeleteSelectedCustomer(view.FocusedRowHandle)
		End Sub
		Private Sub Edit_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			EditSelectedCustomer(view.FocusedRowHandle)
		End Sub
		Private Sub view_RowDoubleClick(ByVal sender As Object, ByVal e As RowDoubleClickEventArgs)
			EditSelectedCustomer(e.HitInfo.RowHandle)
		End Sub
		Private Sub view_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Input.KeyEventArgs)
			If e.Key = System.Windows.Input.Key.Delete Then
				DeleteSelectedCustomer(view.FocusedRowHandle)
			End If
			If e.Key = System.Windows.Input.Key.Enter Then
				EditSelectedCustomer(view.FocusedRowHandle)
			End If
		End Sub

		Private Function GetCustomerIDByRowHandle(ByVal rowHandle As Integer) As String
			Return CStr(grid.GetCellValue(rowHandle, colCustomerID))
		End Function
		Private Sub FindCustomerByIDAndProcess(ByVal customerID As String, ByVal action As Action(Of Customers))
			Dim query As DataServiceQuery(Of Customers) = CType(context.Customers.Where(Function(customer) customer.CustomerID = customerID), DataServiceQuery(Of Customers))
			Try
				query.BeginExecute(AddressOf FindCustomerByIDCallback, New QueryAction(query, action))
			Catch ex As Exception
				HandleException(ex)
			End Try
		End Sub
		Private Sub FindCustomerByIDCallback(ByVal ar As IAsyncResult)
			Dim state As QueryAction = CType(ar.AsyncState, QueryAction)
			Dim customers As IEnumerable(Of Customers) = state.Query.EndExecute(ar)
			Dim action = state.Action
			For Each customer As Customers In customers
				Try
					Dim customerCopy As Customers = customer
					uiDispatcher.BeginInvoke(Function() AnonymousMethod3(action, customerCopy))
				Catch ex As Exception
					HandleException(ex)
				End Try
			Next customer
		End Sub
		Private Function AnonymousMethod3(ByVal action As Action(Of Customers), ByVal customer As Customers) As Boolean
			action(customer)
			Return True
		End Function

		Private Function CreateNewCustomer() As Customers
			Dim newCustomer As New Customers()
			newCustomer.CustomerID = GenerateCustomerID()
			Return newCustomer
		End Function
		Private Function GenerateCustomerID() As String
			Const IDLength As Integer = 5
			Dim result As String = String.Empty
			Dim rnd As New Random()
			For i As Integer = 0 To IDLength - 1
				result &= Convert.ToChar(rnd.Next(65, 90))
			Next i
			Return result
		End Function
		Private Sub DeleteSelectedCustomer(ByVal rowHandle As Integer)
			If rowHandle < 0 Then
				Return
			End If
			If MessageBox.Show("Do you really want to delete the selected customer?", "Delete Customer", MessageBoxButton.OKCancel) <> MessageBoxResult.OK Then
				Return
			End If
			FindCustomerByIDAndProcess(GetCustomerIDByRowHandle(rowHandle), Function(customer) AnonymousMethod1(customer))
		End Sub
		
		Private Function AnonymousMethod1(ByVal customer As Customers) As Boolean
			context.DeleteObject(customer)
			SaveChandes()
			Return True
		End Function
		Private Sub EditSelectedCustomer(ByVal rowHandle As Integer)
			If rowHandle < 0 Then
				Return
			End If
			FindCustomerByIDAndProcess(GetCustomerIDByRowHandle(rowHandle), Function(customer) AnonymousMethod2(customer))
		End Sub
		
		Private Function AnonymousMethod2(ByVal customer As Customers) As Boolean
			customerToEdit = customer
			EditCustomer(customerToEdit, "Edit customer", AddressOf CloseEditCustomerHandler)
			Return True
		End Function
		Private Sub EditCustomer(ByVal customer As Customers, ByVal windowTitle As String, ByVal closedDelegate As EventHandler)
			control = New ContentControl With {.Template = CType(Resources("EditRecordTemplate"), ControlTemplate)}
			control.DataContext = customer

			Dim dialog As DXDialog = New DXDialog(windowTitle, DialogButtons.OkCancel)
			dialog.Content = control
			AddHandler dialog.Closed, closedDelegate
			dialog.ShowDialog()
		End Sub
		Private Sub CloseAddNewCustomerHandler(ByVal sender As Object, ByVal e As EventArgs)
			If CType(sender, DXDialog).DialogResult = DialogResult.OK Then
				context.AddToCustomers(newCustomer)
				SaveChandes()
			End If
			control = Nothing
			newCustomer = Nothing
		End Sub
		Private Sub CloseEditCustomerHandler(ByVal sender As Object, ByVal e As EventArgs)
			If CType(sender, DXDialog).DialogResult = DialogResult.OK Then
				context.UpdateObject(customerToEdit)
				SaveChandes()
			End If
			control = Nothing
			customerToEdit = Nothing
		End Sub

		Private Sub SaveChandes()
			Dim asyncResult As IAsyncResult = Nothing
			Try
				asyncResult = context.BeginSaveChanges(AddressOf SaveCallback, Nothing)
			Catch ex As Exception
				context.CancelRequest(asyncResult)
				HandleException(ex)
			End Try
		End Sub
		Private Sub SaveCallback(ByVal asyncResult As IAsyncResult)
			Dim response As DataServiceResponse = Nothing
			Try
				response = context.EndSaveChanges(asyncResult)
			Catch ex As Exception
				uiDispatcher.BeginInvoke(Function() AnonymousMethod4(context, asyncResult))
				HandleException(ex)
				uiDispatcher.BeginInvoke(Function() AnonymousMethod5())
			End Try
			uiDispatcher.BeginInvoke(Function() AnonymousMethod6(wcfInstantSource))
		End Sub
		Private Sub DetachFailedEntities()
			For Each entityDescriptor As EntityDescriptor In context.Entities
				If entityDescriptor.State <> EntityStates.Unchanged Then
					context.Detach(entityDescriptor.Entity)
				End If
			Next entityDescriptor
		End Sub
		Private Sub HandleException(ByVal ex As Exception)
			uiDispatcher.BeginInvoke(Function() MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK))
		End Sub
		Private Function AnonymousMethod4(ByVal context As NorthwindEntities, ByVal asyncResult As IAsyncResult) As Boolean
			context.CancelRequest(asyncResult)
			Return True
		End Function
		Private Function AnonymousMethod5() As Boolean
			DetachFailedEntities()
			Return True
		End Function
		Private Function AnonymousMethod6(ByVal wcfInstantSource As WcfInstantFeedbackDataSource) As Boolean
			wcfInstantSource.Refresh()
			Return True
		End Function
	End Class

	Public Class QueryAction
		Private query_Renamed As DataServiceQuery(Of Customers)
		Private action_Renamed As Action(Of Customers)

		Public Sub New(ByVal query As DataServiceQuery(Of Customers), ByVal action As Action(Of Customers))
			Me.query_Renamed = query
			Me.action_Renamed = action
		End Sub
		Public ReadOnly Property Query() As DataServiceQuery(Of Customers)
			Get
				Return query_Renamed
			End Get
		End Property
		Public ReadOnly Property Action() As Action(Of Customers)
			Get
				Return action_Renamed
			End Get
		End Property
	End Class
End Namespace
