using System;
using System.Windows.Input;

namespace ATLASDocGenerator.Commands
{
    /// <summary>
    /// Implementation de ICommand pour relayer l'exécution des actions de l'interface utilisateur à des methodes de plugin.
    /// Utilisé par les boutons du ruban de Flare.
    /// Transmet une méthode simple en tant que commande à l'API du ruban.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute; // Action exécutée qd commande déclenchée 
        private readonly Predicate<object> _canExecute; // Condition d'exécution de la commande, si aucune condition donnée, alors la commande est toujours exécutable

        // Action toujours exécutable
        public RelayCommand(Action<object> execute)
            : this(execute, null)
        {
        }
        // Crée une commande avec action exécutable + condition optionelle
        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");

            _execute = execute;
            _canExecute = canExecute;
        }

        // Rtourne True si la commande peut s'exécuter, False sinon
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }
        // Exécute l'action de la commande
        public void Execute(object parameter)
        {
            _execute(parameter);
        }
        // Refresh Command availabiity
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}