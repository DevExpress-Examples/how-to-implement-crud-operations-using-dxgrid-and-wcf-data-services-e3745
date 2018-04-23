using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using WCFInstantFeedbackCRUD.NorthwindService;

namespace WCFInstantFeedbackCRUD {
    public partial class MainPage : UserControl {
        NorthwindEntities context;
        Control control;
        Customers newCustomer;
        Customers customerToEdit;
        Dispatcher uiDispatcher;

        public MainPage() {
            InitializeComponent();
            uiDispatcher = grid.Dispatcher;
            context = new NorthwindEntities(new Uri("NorthwindService.svc/", UriKind.Relative));
            wcfInstantSource.DataServiceContext = context;
            wcfInstantSource.Query = context.Customers;
        }

        private void Add_Click(object sender, RoutedEventArgs e) {
            newCustomer = CreateNewCustomer();
            EditCustomer(newCustomer, "Add new customer", CloseAddNewCustomerHandler);
        }
        private void Remove_Click(object sender, RoutedEventArgs e) {
            DeleteSelectedCustomer(view.FocusedRowHandle);
        }
        private void Edit_Click(object sender, RoutedEventArgs e) {
            EditSelectedCustomer(view.FocusedRowHandle);
        }
        private void view_RowDoubleClick(object sender, RowDoubleClickEventArgs e) {
            EditSelectedCustomer(e.HitInfo.RowHandle);
        }
        private void view_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            if(e.Key == System.Windows.Input.Key.Delete) {
                DeleteSelectedCustomer(view.FocusedRowHandle);
            }
            if(e.Key == System.Windows.Input.Key.Enter) {
                EditSelectedCustomer(view.FocusedRowHandle);
            }
        }

        string GetCustomerIDByRowHandle(int rowHandle) {
            return (string)grid.GetCellValue(rowHandle, colCustomerID);
        }
        void FindCustomerByIDAndProcess(string customerID, Action<Customers> action) {
            DataServiceQuery<Customers> query = (DataServiceQuery<Customers>)context.Customers.Where<Customers>(customer => customer.CustomerID == customerID);
            try {
                query.BeginExecute(FindCustomerByIDCallback, new QueryAction(query, action));
            } catch(Exception ex) {
                HandleException(ex);
            }
        }
        void FindCustomerByIDCallback(IAsyncResult ar) {
            QueryAction state = (QueryAction)ar.AsyncState;
            IEnumerable<Customers> customers = state.Query.EndExecute(ar);
            foreach(Customers customer in customers) {
                try {
                    uiDispatcher.BeginInvoke(() => state.Action(customer));
                } catch(Exception ex) {
                    HandleException(ex);
                }
            }
        }

        Customers CreateNewCustomer() {
            Customers newCustomer = new Customers();
            newCustomer.CustomerID = GenerateCustomerID();
            return newCustomer;
        }
        string GenerateCustomerID() {
            const int IDLength = 5;
            string result = String.Empty;
            Random rnd = new Random();
            for(int i = 0; i < IDLength; i++) {
                result += Convert.ToChar(rnd.Next(65, 90));
            }
            return result;
        }
        void DeleteSelectedCustomer(int rowHandle) {
            if(rowHandle < 0) return;
            if(MessageBox.Show("Do you really want to delete the selected customer?", "Delete Customer", MessageBoxButton.OKCancel) != MessageBoxResult.OK) return;
            FindCustomerByIDAndProcess(GetCustomerIDByRowHandle(rowHandle), customer => { context.DeleteObject(customer); SaveChandes(); });
        }
        void EditSelectedCustomer(int rowHandle) {
            if(rowHandle < 0) return;
            FindCustomerByIDAndProcess(GetCustomerIDByRowHandle(rowHandle), customer => {
                customerToEdit = customer; EditCustomer(customerToEdit, "Edit customer", CloseEditCustomerHandler);
            });
        }
        void EditCustomer(Customers customer, string windowTitle, EventHandler closedDelegate) {
            control = new ContentControl { Template = (ControlTemplate)Resources["EditRecordTemplate"] };
            control.DataContext = customer;

            DXDialog dialog = new DXDialog(windowTitle, DialogButtons.OkCancel);
            dialog.Content = control;
            dialog.Closed += closedDelegate;
            dialog.ShowDialog();
        }
        void CloseAddNewCustomerHandler(object sender, EventArgs e) {
            if(((DXDialog)sender).DialogResult == DialogResult.OK) {
                context.AddToCustomers(newCustomer);
                SaveChandes();
            }
            control = null;
            newCustomer = null;
        }
        void CloseEditCustomerHandler(object sender, EventArgs e) {
            if(((DXDialog)sender).DialogResult == DialogResult.OK) {
                context.UpdateObject(customerToEdit);
                SaveChandes();
            }
            control = null;
            customerToEdit = null;
        }

        void SaveChandes() {
            IAsyncResult asyncResult = null;
            try {
                asyncResult = context.BeginSaveChanges(SaveCallback, null);
            } catch(Exception ex) {
                context.CancelRequest(asyncResult);
                HandleException(ex);
            }
        }
        void SaveCallback(IAsyncResult asyncResult) {
            DataServiceResponse response = null;
            try {
                response = context.EndSaveChanges(asyncResult);
            } catch(Exception ex) {
                uiDispatcher.BeginInvoke(() => context.CancelRequest(asyncResult));
                HandleException(ex);
                uiDispatcher.BeginInvoke(() => DetachFailedEntities());
            }
            uiDispatcher.BeginInvoke(() => wcfInstantSource.Refresh());
        }
        void DetachFailedEntities() {
            foreach(EntityDescriptor entityDescriptor in context.Entities) {
                if(entityDescriptor.State != EntityStates.Unchanged) {
                    context.Detach(entityDescriptor.Entity);
                }
            }
        }
        void HandleException(Exception ex) {
            uiDispatcher.BeginInvoke(() => MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK));
        }
    }

    public class QueryAction {
        DataServiceQuery<Customers> query;
        Action<Customers> action;

        public QueryAction(DataServiceQuery<Customers> query, Action<Customers> action) {
            this.query = query;
            this.action = action;
        }
        public DataServiceQuery<Customers> Query { get { return query; } }
        public Action<Customers> Action { get { return action; } }
    }
}
