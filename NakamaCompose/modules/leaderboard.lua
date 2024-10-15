local nakama = require("nakama")

local function create_leaderboard(context, payload)
    local leaderboard_id = "testLeaderboard"
    local authoritative = false
    local sort_order = "desc"  -- Sort scores in descending order
    local operator = "best"  -- Use "best" operator to track the best score
    local reset_schedule = "0 0 * * 1"  -- Optional: resets every Monday at midnight
    local metadata = {}  -- Optional: add any custom metadata
    
    local success, err = pcall(function()
        nakama.leaderboard_create(leaderboard_id, authoritative, sort_order, operator, reset_schedule, metadata)
    end)
    
    if success then
        return {message = "Leaderboard created successfully!"}
    else
        return {error = err}
    end
end

nakama.register_rpc(create_leaderboard, "create_leaderboard")