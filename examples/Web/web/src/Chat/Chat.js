import React, { Component, createRef } from 'react';
import { activeChatKey } from '../config';
import api from '../api';
import './Chat.css';
import {
    Segment,
    List, Input, Card, Icon, Ref, Dimmer, Loader
} from 'semantic-ui-react';
import ChatMenu from './ChatMenu';

const initialState = {
    active: '',
    conversations: {},
    interval: undefined,
    loading: false
};

class Chat extends Component {
    state = initialState;
    messageRef = undefined;
    listRef = createRef();

    componentDidMount = async () => {
        await this.fetchConversations();

        const names = [...Object.keys(this.state.conversations)];

        this.setState({ 
            active: sessionStorage.getItem(activeChatKey) || names.length > 0 ? names[0] : '',
            interval: window.setInterval(this.fetchConversations, 5000),
            loading: true
        }, () => this.selectConversation(this.state.active));
    }

    componentWillUnmount = () => {
        clearInterval(this.state.interval);
        this.setState({ interval: undefined });
    }

    fetchConversations = async () => {
        const { active } = this.state;
        let conversations = (await api.get('/conversations')).data;

        const unAckedActiveMessages = (conversations[active] || [])
            .filter(message => !message.acknowledged);

        if (unAckedActiveMessages.length > 0) {
            await this.acknowledgeMessages(active, { force: true });
            conversations = {
                ...conversations, 
                [active]: conversations[active].map(message => ({...message, acknowledged: true }))
            };
        };

        this.setState({ conversations }, () => {
            this.acknowledgeMessages(this.state.active);
        });
    }

    acknowledgeMessages = async (username, { force = false } = {}) => {
        if (!username) return;

        const unAckedMessages = (this.state.conversations[username] || [])
            .filter(message => !message.acknowledged);

        if (!force && unAckedMessages.length === 0) return;

        await api.put(`/conversations/${username}`);
    }

    sendMessage = async (username, message) => {
        await api.post(`/conversations/${username}`, JSON.stringify(message));
    }

    sendReply = async () => {
        const { active } = this.state;
        const message = this.messageRef.current.value;

        if (!this.validInput()) {
            return;
        }

        await this.sendMessage(active, message);
        this.messageRef.current.value = '';
    }

    initiateMessage = async (username, message) => {
        await this.sendMessage(username, message);

        this.setState({ 
            conversations: {
                ...this.state.conversations, 
                username: [message]
            },
            active: username
        });
    }

    validInput = () => (this.state.active || '').length > 0 && ((this.messageRef && this.messageRef.current && this.messageRef.current.value) || '').length > 0;

    focusInput = () => {
        this.messageRef.current.focus();
    }

    formatTimestamp = (timestamp) => {
        const date = new Date(timestamp);
        const dtfUS = new Intl.DateTimeFormat('en', { 
            month: 'numeric', 
            day: 'numeric',
            hour: 'numeric',
            minute: '2-digit'
        });

        return dtfUS.format(date);
    }

    selectConversation = (username) => {
        this.setState({ 
            active: username,
            loading: true
        }, async () => {
            const { active } = this.state;

            sessionStorage.setItem(activeChatKey, active);

            const tasks = [this.fetchConversations(), this.acknowledgeMessages(active)];
            await Promise.all(tasks);

            try {
                this.listRef.current.lastChild.scrollIntoView({ behavior: 'smooth' });
            } catch {}

            this.setState({ loading: false });
        });
    }

    deleteConversation = async (username) => {
        await api.delete(`/conversations/${username}`);

        const { conversations } = this.state;
        delete conversations[username];

        this.setState({ 
            active: initialState.active,
            conversations
        }, () => {
            sessionStorage.removeItem(activeChatKey);
        });
    }

    render = () => {
        const { conversations, active, loading } = this.state;
        const messages = conversations[active] || [];

        return (
            <div className='chats'>
                <Segment raised>
                    <ChatMenu
                        conversations={conversations}
                        active={active}
                        onConversationChange={(name) => this.selectConversation(name)}
                        initiateMessage={this.initiateMessage}
                    />
                </Segment>
                {active && <Card className='chat-active-card' raised>
                    <Card.Content onClick={() => this.focusInput()}>
                        <Card.Header>
                            <Icon name='circle' color='green'/>
                            {active}
                            <Icon 
                                className='close-button' 
                                name='close' 
                                color='red' 
                                link
                                onClick={() => this.deleteConversation(active)}
                            />
                        </Card.Header>
                        <div className='chat'>
                        {loading ? <Dimmer active inverted><Loader inverted/></Dimmer> :
                            <Segment.Group>
                                <Segment className='chat-history'>
                                    <Ref innerRef={this.listRef}>
                                        <List>
                                            {messages.map((message, index) => 
                                                <List.Content 
                                                    key={index}
                                                    className={`chat-message ${message.username !== active ? 'chat-message-self' : ''}`}
                                                >
                                                    <span className='chat-message-time'>{this.formatTimestamp(message.timestamp)}</span>
                                                    <span className='chat-message-name'>{message.username}: </span>
                                                    <span className='chat-message-message'>{message.message}</span>
                                                </List.Content>
                                            )}
                                            <List.Content id='chat-history-scroll-anchor'/>
                                        </List>
                                    </Ref>
                                </Segment>
                                <Segment className='chat-input'>
                                    <Input
                                        fluid
                                        transparent
                                        input={<input id='chat-message-input' type="text" data-lpignore="true" autoComplete="off"></input>}
                                        ref={input => this.messageRef = input && input.inputRef}
                                        action={{ 
                                            icon: <Icon name='send' color='green'/>, 
                                            className: 'chat-message-button', onClick: this.sendMessage,
                                            disabled: !this.validInput()
                                        }}
                                        onKeyUp={(e) => e.key === 'Enter' ? this.sendReply() : ''}
                                    />
                                </Segment>
                            </Segment.Group>}
                        </div>
                    </Card.Content>
                </Card>}
            </div>
        )
    }
}

export default Chat;