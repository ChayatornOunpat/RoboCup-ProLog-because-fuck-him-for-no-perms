:- use_module(library(http/thread_httpd)).
:- use_module(library(http/http_dispatch)).
:- use_module(library(http/http_json)).

:- consult('game.pl').  % <-- your existing file

:- http_handler(root(action), handle_action, []).

start_server :-
    http_server(http_dispatch, [port(5000)]).

handle_action(Request) :-
    http_read_json_dict(Request, Dict),
    Action = Dict.get(action),
    process_action(Action, Response),
    reply_json_dict(Response).

% ================================
% ACTION HANDLER
% ================================
% Bridge between Unity and your game
% Unity sends step -> Prolog runs: simulate_half(1,1) -> Then returns: ball position, players, possession

process_action("step", Response) :-
    with_output_to(string(_), simulate_half(1,1)),  % run one tick

    % get ball
    ball(X, Y, _, _, Possession),

    % get players
    findall(_{name: Name, team: Team, x: PX, y: PY},
        player(Team, Name, _, _, PX, PY),
        Players),

    Response = _{
        ball: [X, Y],
        possession: Possession,
        players: Players
    }.

process_action("reset", Response) :-
    reset_players_ball,
    build_game_state(Response).



