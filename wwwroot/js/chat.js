document.addEventListener('DOMContentLoaded', function() {
    const chatForm = document.getElementById('chat-form');
    const chatInput = document.getElementById('chat-input');
    const chatMessages = document.getElementById('chat-messages');
    const chatOptions = document.getElementById('chat-options');
    
    // URL de l'API du chatbot
    const botUrl = '/api/webchat/messages';
    
    // Identifiant unique pour l'utilisateur (généré aléatoirement pour cette session)
    const userId = 'web_user_' + Math.random().toString(36).substring(2, 15);
    
    // Envoyer un message de bienvenue au chargement
    setTimeout(function() {
        addBotMessage("Bonjour ! Je suis votre assistant virtuel pour vous aider à préparer vos entretiens d'embauche. Pour mieux vous accompagner, j'aimerais d'abord mieux vous connaître.");
        showOptions([
            "👤 Évaluer mon profil",
            "📋 Commencer la préparation",
            "💬 Simuler un entretien",
            "📊 Voir mon feedback",
            "❓ Conseils généraux",
            "⚙️ Mettre à jour mon profil"
        ]);
    }, 500);
    
    // Gérer l'envoi du formulaire
    chatForm.addEventListener('submit', function(e) {
        e.preventDefault();
        
        const message = chatInput.value.trim();
        if (!message) return;
        
        // Ajouter le message de l'utilisateur à la conversation
        addUserMessage(message);
        
        // Effacer le champ de saisie
        chatInput.value = '';
        
        // Afficher l'indicateur de frappe
        showTyping();
        
        // Envoyer le message au bot
        sendMessageToBot(message);
    });
    
    // Gérer les clics sur les options
    chatOptions.addEventListener('click', function(e) {
        if (e.target.classList.contains('option-button')) {
            const message = e.target.textContent;
            
            // Ajouter le message de l'utilisateur à la conversation
            addUserMessage(message);
            
            // Afficher l'indicateur de frappe
            showTyping();
            
            // Envoyer le message au bot
            sendMessageToBot(message);
            
            // Effacer les options précédentes
            chatOptions.innerHTML = '';
        }
    });
    
    // Fonction pour ajouter un message de l'utilisateur
    function addUserMessage(message) {
        const messageElement = document.createElement('div');
        messageElement.classList.add('message', 'user-message');
        messageElement.innerHTML = `
            <div class="message-content">${message}</div>
        `;
        chatMessages.appendChild(messageElement);
        scrollToBottom();
    }
    
    // Fonction pour ajouter un message du bot
    function addBotMessage(message) {
        // Supprimer l'indicateur de frappe
        removeTyping();
        
        const messageElement = document.createElement('div');
        messageElement.classList.add('message', 'bot-message');
        messageElement.innerHTML = `
            <div class="message-avatar">
                <img src="https://cdn-icons-png.flaticon.com/512/4205/4205906.png" alt="Bot Avatar">
            </div>
            <div class="message-content">${message}</div>
        `;
        chatMessages.appendChild(messageElement);
        scrollToBottom();
    }
    
    // Fonction pour afficher l'indicateur de frappe
    function showTyping() {
        // S'assurer qu'il n'y a pas déjà un indicateur de frappe
        removeTyping();
        
        const typingElement = document.createElement('div');
        typingElement.classList.add('message', 'bot-message', 'typing-indicator');
        typingElement.innerHTML = `
            <div class="message-avatar">
                <img src="https://cdn-icons-png.flaticon.com/512/4205/4205906.png" alt="Bot Avatar">
            </div>
            <div class="message-content">
                <div class="typing">
                    <span></span>
                    <span></span>
                    <span></span>
                </div>
            </div>
        `;
        chatMessages.appendChild(typingElement);
        scrollToBottom();
    }
    
    // Fonction pour supprimer l'indicateur de frappe
    function removeTyping() {
        const typingIndicator = document.querySelector('.typing-indicator');
        if (typingIndicator) {
            typingIndicator.remove();
        }
    }
    
    // Fonction pour afficher les options/boutons
    function showOptions(options) {
        // Effacer les options précédentes
        chatOptions.innerHTML = '';
        
        options.forEach(function(option) {
            const button = document.createElement('button');
            button.classList.add('option-button');
            button.textContent = option;
            chatOptions.appendChild(button);
        });
    }
    
    // Fonction pour faire défiler la conversation vers le bas
    function scrollToBottom() {
        chatMessages.scrollTop = chatMessages.scrollHeight;
    }
    
    // Fonction pour envoyer un message au bot
    function sendMessageToBot(message) {
        const payload = {
            text: message,
            userId: userId
        };
        
        fetch(botUrl, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        })
        .then(response => {
            if (!response.ok) {
                throw new Error('Erreur réseau: ' + response.status);
            }
            return response.json();
        })
        .then(data => {
            // Ajouter la réponse du bot
            addBotMessage(data.text);
            
            // Si des boutons d'options sont inclus dans la réponse
            if (data.suggestedActions && data.suggestedActions.actions) {
                const options = data.suggestedActions.actions.map(action => action.title);
                showOptions(options);
            }
        })
        .catch(error => {
            console.error('Erreur lors de la communication avec le bot:', error);
            removeTyping();
            addBotMessage("Désolé, j'ai rencontré un problème technique. Veuillez réessayer plus tard.");
            showOptions(["🏠 Menu principal"]);
        });
    }
}); 